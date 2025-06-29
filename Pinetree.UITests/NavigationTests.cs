using Microsoft.Playwright;

namespace Pinetree.UITests
{
    [TestClass]
    public class NavigationTests : PlaywrightTestBase
    {
        const int Timeout = 2000;

        [TestMethod]
        public async Task ShouldNavigateToTryitAndAddChildElements()
        {
            await Page.GotoAsync(TargetUrl);
            StringAssert.Contains(await Page.TitleAsync(), "Pinetree");

            await Page.ClickAsync("a[href='/play']");
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Page.WaitForTimeoutAsync(Timeout);
            await Page.WaitForSelectorAsync(".panel-footer div", new() { Timeout = Timeout });

            StringAssert.Contains(await Page.TitleAsync(), "Playground");

            var initialFileCount = await Page.TextContentAsync(".panel-footer div") ?? "";
            Assert.IsTrue(initialFileCount.Contains("FileCount : 1"));

            await Page.ClickAsync("button[title='Add Child Item']");
            await Page.WaitForTimeoutAsync(Timeout);
            await Page.WaitForSelectorAsync(".panel-footer div:has-text('FileCount : 2')", new() { Timeout = Timeout });

            var updatedFileCount = await Page.TextContentAsync(".panel-footer div") ?? "";
            Assert.IsTrue(updatedFileCount.Contains("FileCount : 2"));

            await Page.ClickAsync("button[title='Add Child Item']");
            await Page.WaitForTimeoutAsync(Timeout);
            await Page.WaitForSelectorAsync(".panel-footer div:has-text('FileCount : 3')", new() { Timeout = Timeout });

            var finalFileCount = await Page.TextContentAsync(".panel-footer div") ?? "";
            Assert.IsTrue(finalFileCount.Contains("FileCount : 3"));

            var childItems = await Page.QuerySelectorAllAsync("ul li ul li .title");
            if (childItems.Count > 0)
            {
                await childItems[0].ClickAsync();
            }

            Page.Dialog += (_, dialog) =>
            {
                if (dialog.Type == "confirm" &&
                    dialog.Message.Contains("Are you sure you want to delete"))
                {
                    dialog.AcceptAsync();
                }
            };

            await Page.ClickAsync("ul li ul li button[title='Delete Item']");
            await Page.WaitForTimeoutAsync(Timeout);
            await Page.WaitForSelectorAsync(".panel-footer div:has-text('FileCount : 2')", new() { Timeout = Timeout });

            var afterDeletionCount = await Page.TextContentAsync(".panel-footer div") ?? "";
            Assert.IsTrue(afterDeletionCount.Contains("FileCount : 2"));
        }

        [TestMethod]
        public async Task ShouldCreateFileWithImageUploadAndCleanup()
        {
            // Step 1: Login
            await LoginAsTestUser();

            // Step 2: Navigate to User page and create new file
            await Page.GotoAsync($"{TargetUrl}/files");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Click on "Create New File" card
            await Page.ClickAsync("div.card-body:has-text('Create New File')");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.WaitForTimeoutAsync(Timeout);

            // Verify we're on the edit page
            StringAssert.Contains(Page.Url, "/Edit/");
            var currentUrl = Page.Url;
            var guidFromUrl = currentUrl.Substring(currentUrl.LastIndexOf('/') + 1);

            // Step 3: Add title to the file
            await Page.FillAsync("input[placeholder='Untitled']", "Test File with Image");
            await Page.WaitForTimeoutAsync(500);

            // Step 4: Upload image using file input
            var testImagePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Pinetree", "wwwroot", "Pinetree.png");
            if (!File.Exists(testImagePath))
            {
                // Fallback to any available image
                testImagePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Pinetree", "wwwroot", "favicon.png");
            }
            Assert.IsTrue(File.Exists(testImagePath), $"Test image not found at: {testImagePath}");

            // Wait for the page to be fully loaded
            await Page.WaitForSelectorAsync("button[title='Insert Image']", new() { Timeout = 5000 });

            try
            {
                // Handle file dialog and upload
                var fileChooser = await Page.RunAndWaitForFileChooserAsync(async () =>
                {
                    // Click the insert image button in the toolbar to trigger file dialog
                    await Page.ClickAsync("button[title='Insert Image']");
                });
                await fileChooser.SetFilesAsync(testImagePath);
            }
            catch (TimeoutException)
            {
                // Fallback: Try to trigger file input directly if toolbar method fails
                await Page.SetInputFilesAsync("input[type='file']", testImagePath);
            }

            // Wait for image to be processed and inserted into textarea
            await Page.WaitForTimeoutAsync(2000);

            // Verify image markdown is inserted
            var textareaContent = await Page.InputValueAsync("textarea");
            Assert.IsTrue(textareaContent.Contains("!["), "Image markdown should be inserted");
            Assert.IsTrue(textareaContent.Contains("](blob:"), "Image should have blob URL");            // Step 5: Save the file
            await Page.ClickAsync("button:has-text('Save')");

            // Wait for save operation with multiple approaches
            bool saveCompleted = false;
            int maxAttempts = 10;

            for (int attempt = 0; attempt < maxAttempts && !saveCompleted; attempt++)
            {
                await Page.WaitForTimeoutAsync(1000);

                try
                {
                    // Check if save button is no longer showing "Saving..."
                    var saveButton = await Page.QuerySelectorAsync("button[title='Save changes']");
                    if (saveButton != null)
                    {
                        var buttonText = await saveButton.TextContentAsync();
                        var isDisabled = await saveButton.GetAttributeAsync("disabled");

                        if (!buttonText?.Contains("Saving...") == true)
                        {
                            Console.WriteLine($"Save completed after {attempt + 1} seconds - button text: '{buttonText}', disabled: {isDisabled}");
                            saveCompleted = true;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Save check attempt {attempt + 1} failed: {ex.Message}");
                }
            }

            if (!saveCompleted)
            {
                Console.WriteLine("Warning: Could not confirm save completion, continuing with test");
            }

            // Additional wait to ensure save is fully processed
            await Page.WaitForTimeoutAsync(2000);

            // Verify URL is updated (should be the same since it's an existing file)
            var updatedUrl = Page.Url;
            Assert.AreEqual(currentUrl, updatedUrl, "URL should remain the same after save");

            // Step 6: Go back to Files page and then return to edit to verify Save button is disabled
            await Page.ClickAsync("button.list-group-item:has-text('Files')");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.WaitForTimeoutAsync(Timeout);

            // Find the created file and click to edit again
            var allCardsBeforeReEdit = await Page.QuerySelectorAllAsync(".card");
            IElementHandle? targetCardForReEdit = null;

            foreach (var card in allCardsBeforeReEdit)
            {
                try
                {
                    var titleElement = await card.QuerySelectorAsync("strong.card-title");
                    if (titleElement != null)
                    {
                        var title = await titleElement.TextContentAsync();
                        if (title?.Contains("Test File with Image") == true)
                        {
                            targetCardForReEdit = card;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error finding card for re-edit: {ex.Message}");
                }
            }
            Assert.IsNotNull(targetCardForReEdit, "Created test file should be found for re-editing");

            // Debug: Print card structure to understand available elements
            var cardHtml = await targetCardForReEdit.InnerHTMLAsync();
            Console.WriteLine($"Card HTML structure: {cardHtml}");

            // Click on the edit link/button within the card to enter edit mode again
            var editLink = await targetCardForReEdit.QuerySelectorAsync("a[href*='/Edit/']");
            if (editLink == null)
            {
                // Fallback: try to find edit button or icon
                editLink = await targetCardForReEdit.QuerySelectorAsync("button:has-text('Edit')");
                if (editLink == null)
                {
                    editLink = await targetCardForReEdit.QuerySelectorAsync("i.bi-pencil");
                    if (editLink == null)
                    {
                        // Last resort: click on the card title which might be a link
                        editLink = await targetCardForReEdit.QuerySelectorAsync("strong.card-title");
                    }
                }
            }
            Assert.IsNotNull(editLink, "Edit link or button should be found within the card");
            await editLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.WaitForTimeoutAsync(Timeout);

            // Verify we're back on the edit page with a more robust check
            var editPageUrl = Page.Url;
            StringAssert.Contains(editPageUrl, "/Edit/", "Should be on edit page after clicking edit link");

            // Wait for the edit page to fully load
            await Page.WaitForSelectorAsync("button[title='Save changes']", new() { Timeout = 5000 });
            await Page.WaitForSelectorAsync("textarea", new() { Timeout = 5000 });

            Console.WriteLine($"Successfully navigated to edit page: {editPageUrl}");

            // Step 7: Verify Save button is disabled (no pending changes)
            var saveButtonAfterReEntry = await Page.QuerySelectorAsync("button[title='Save changes']");
            Assert.IsNotNull(saveButtonAfterReEntry, "Save button should be present");

            var isDisabledAfterReEntry = await saveButtonAfterReEntry.GetAttributeAsync("disabled");
            Assert.IsNotNull(isDisabledAfterReEntry, "Save button should be disabled when no changes are made");
            Console.WriteLine("✓ Save button is correctly disabled after re-entering edit mode");

            // Step 8: Go back to file list for deletion
            await Page.ClickAsync("button.list-group-item:has-text('Files')");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.WaitForTimeoutAsync(Timeout);

            // Wait for the page to load completely
            await Page.WaitForSelectorAsync(".card:has(.card-title)", new() { Timeout = 10000 });

            // Step 9: Find and delete the test file
            var allCards = await Page.QuerySelectorAllAsync(".card");
            IElementHandle? targetCard = null;

            Console.WriteLine($"Found {allCards.Count} cards total");

            foreach (var card in allCards)
            {
                try
                {
                    var titleElement = await card.QuerySelectorAsync("strong.card-title");
                    if (titleElement != null)
                    {
                        var title = await titleElement.TextContentAsync();
                        Console.WriteLine($"Found card with title: '{title}'");

                        if (title?.Contains("Test File with Image") == true)
                        {
                            targetCard = card;
                            Console.WriteLine("Found target card for deletion");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking card: {ex.Message}");
                }
            }

            Assert.IsNotNull(targetCard, "Created test file should be found in the list");
            
            // Find the three-dots menu button (try both parent button and icon)
            var menuButton = await targetCard.QuerySelectorAsync("button[data-bs-toggle='dropdown']");
            if (menuButton == null)
            {
                // Fallback: try to find by the icon directly
                var menuIcon = await targetCard.QuerySelectorAsync("i.bi-three-dots-vertical");
                if (menuIcon != null)
                {
                    // Get the parent button
                    menuButton = await menuIcon.QuerySelectorAsync("xpath=..");
                }
            }
            Assert.IsNotNull(menuButton, "Menu button should be found");
            
            // Click the menu button to open dropdown
            await menuButton.ClickAsync();
            Console.WriteLine("Menu button clicked");
            
            // Wait for dropdown menu to appear
            await Page.WaitForTimeoutAsync(500);
            await Page.WaitForSelectorAsync(".dropdown-menu.show", new() { Timeout = 2000 });
            
            // Find and click the delete item in the dropdown menu
            var deleteMenuItem = await Page.QuerySelectorAsync(".dropdown-item.text-danger");
            Assert.IsNotNull(deleteMenuItem, "Delete menu item should be found");

            // Handle confirm dialog - set up handler before clicking
            bool dialogHandled = false;
            EventHandler<IDialog> dialogHandler = async (sender, dialog) =>
            {
                Console.WriteLine($"Dialog appeared: {dialog.Type} - {dialog.Message}");
                if (!dialogHandled && dialog.Type == "confirm" && dialog.Message.Contains("Are you sure you want to delete"))
                {
                    dialogHandled = true;
                    try
                    {
                        await dialog.AcceptAsync();
                        Console.WriteLine("Delete dialog accepted");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error accepting dialog: {ex.Message}");
                    }
                }
            };

            Page.Dialog += dialogHandler;

            try
            {
                await deleteMenuItem.ClickAsync();
                Console.WriteLine("Delete menu item clicked");

                // Wait for dialog to appear and be handled
                await Page.WaitForTimeoutAsync(2000);
            }
            finally
            {
                // Remove dialog handler to prevent conflicts
                Page.Dialog -= dialogHandler;
            }

            // Wait for deletion to complete - look for changes in the page
            await Page.WaitForTimeoutAsync(2000);

            // Wait for potential page updates/re-renders
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Page.WaitForTimeoutAsync(1000);

            // Re-query the page after deletion to get the updated list
            await Page.ReloadAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.WaitForTimeoutAsync(Timeout);

            // Now check if file is removed from list by checking if we can still find it
            var updatedCards = await Page.QuerySelectorAllAsync(".card");
            bool fileStillExists = false;

            Console.WriteLine($"After deletion and page reload, found {updatedCards.Count} cards");

            foreach (var card in updatedCards)
            {
                try
                {
                    var titleElement = await card.QuerySelectorAsync("strong.card-title");
                    if (titleElement != null)
                    {
                        var title = await titleElement.TextContentAsync();
                        Console.WriteLine($"Card after deletion: '{title}'");

                        if (title?.Contains("Test File with Image") == true)
                        {
                            fileStillExists = true;
                            Console.WriteLine("WARNING: Test file still exists after deletion!");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking card after deletion: {ex.Message}");
                }
            }
            Assert.IsFalse(fileStillExists, "Test file should be deleted from the list");

            // Step 10: Clean up images from image tab (optional since file deletion may cascade)
            try
            {
                await Page.ClickAsync("button.list-group-item:has-text('My Image')");
                await Page.WaitForTimeoutAsync(2000);

                // Delete any test images (images uploaded during this test)
                var imageDeleteButtons = await Page.QuerySelectorAllAsync("button:has-text('Delete')");

                if (imageDeleteButtons.Count > 0)
                {
                    // Set up dialog handler for image deletion
                    bool imageDialogHandled = false;
                    EventHandler<IDialog> imageDialogHandler = async (sender, dialog) =>
                    {
                        if (!imageDialogHandled && dialog.Type == "confirm" && dialog.Message.Contains("Are you sure you want to delete"))
                        {
                            imageDialogHandled = true;
                            try
                            {
                                await dialog.AcceptAsync();
                                Console.WriteLine("Image delete dialog accepted");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error accepting image dialog: {ex.Message}");
                            }
                        }
                    };

                    Page.Dialog += imageDialogHandler;

                    try
                    {
                        await imageDeleteButtons[0].ClickAsync();
                        await Page.WaitForTimeoutAsync(2000);
                        Console.WriteLine("Attempted to delete uploaded image");
                    }
                    finally
                    {
                        Page.Dialog -= imageDialogHandler;
                    }
                }
                else
                {
                    Console.WriteLine("No images found to delete - they may have been deleted with the file");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during image cleanup: {ex.Message}");
                // Don't fail the test if image cleanup fails
            }
        }

        private async Task LoginAsTestUser()
        {
            await Page.GotoAsync(TargetUrl);
            await Page.ClickAsync("a[href='Account/Login']");
            await Page.WaitForTimeoutAsync(Timeout);
            await Page.WaitForSelectorAsync("input[id='Input.UserName']", new() { Timeout = Timeout });

            await Page.FillAsync("input[id='Input.UserName']", "test");
            await Page.FillAsync("input[id='Input.Password']", "test");
            await Page.ClickAsync("button[type='submit']");
            await Page.WaitForTimeoutAsync(Timeout);

            // Verify login success by checking if we can access user page
            await Page.GotoAsync($"{TargetUrl}/files");
            await Page.WaitForTimeoutAsync(Timeout);
            await Page.WaitForSelectorAsync(".card", new() { Timeout = Timeout });
            StringAssert.Contains(Page.Url, "/files");
        }
    }
}

// EmojiPicker.razor.ts - TypeScript for the EmojiPicker component
// This file provides JavaScript interop functionality for the EmojiPicker component

// Make this a module by adding an export
export {};

// Define a type for the global window object with our custom methods
declare global {
    interface Window {
        getEmojiKeywords: (emoji: string) => string[];
        focusEmojiSearch: () => void;
        toggleEmojiSearchFocus: (focused: boolean) => void;
        filterEmojis: (query: string) => void;
        showEmojiPicker: (emojiHtml: string) => Promise<string>;
        selectEmoji: (emoji: string) => void;
    }
}

/**
 * Gets keywords associated with an emoji for search functionality
 * @param emoji - The emoji character
 * @returns Array of keywords
 */
function getEmojiKeywords(emoji: string): string[] {
    const emojiKeywords: Record<string, string[]> = {
        '😀': ['grin', 'smile', 'happy', 'joy', 'cheerful'],
        '😃': ['smile', 'happy', 'joy', 'grin'],
        '😄': ['smile', 'happy', 'joy', 'laugh', 'grin'],
        '😁': ['grin', 'smile', 'happy', 'joy'],
        '😆': ['laugh', 'happy', 'smile', 'joy', 'lol'],
        '😅': ['laugh', 'happy', 'smile', 'sweat', 'relief'],
        '🤣': ['laugh', 'lol', 'rofl', 'funny', 'hilarious'],
        '😂': ['laugh', 'cry', 'tears', 'joy', 'funny', 'lol'],
        '🙂': ['smile', 'happy', 'slight'],
        '🙃': ['upside', 'down', 'silly', 'crazy'],
        '😉': ['wink', 'flirt', 'playful'],
        '😊': ['smile', 'happy', 'blush', 'pleased'],
        '😇': ['angel', 'innocent', 'halo', 'good'],
        '🥰': ['love', 'hearts', 'adore', 'crush'],
        '😍': ['love', 'heart', 'eyes', 'crush', 'adore'],
        '🤩': ['star', 'eyes', 'excited', 'amazing'],
        '😘': ['kiss', 'love', 'heart', 'romantic'],
        '😗': ['kiss', 'whistle', 'love'],
        '☺️': ['smile', 'happy', 'pleased'],
        '😚': ['kiss', 'love', 'closed', 'eyes'],
        '😙': ['kiss', 'love', 'whistle'],
        '🥲': ['tear', 'happy', 'sad', 'emotional'],
        '😋': ['yum', 'delicious', 'tongue', 'taste'],
        '😛': ['tongue', 'playful', 'silly'],
        '😜': ['tongue', 'wink', 'playful', 'silly'],
        '🤪': ['crazy', 'wild', 'silly', 'zany'],
        '😝': ['tongue', 'crazy', 'silly', 'playful'],
        '🤑': ['money', 'rich', 'dollar', 'cash'],
        '🤗': ['hug', 'happy', 'arms', 'embrace'],
        '🤭': ['oops', 'giggle', 'surprise', 'shock'],
        '🤫': ['quiet', 'silence', 'hush', 'secret'],
        '🤔': ['think', 'ponder', 'thought', 'confused'],
        '🤐': ['zip', 'silence', 'quiet', 'mouth', 'sealed'],
        '🤨': ['raised', 'eyebrow', 'suspicious', 'doubt'],
        '😐': ['neutral', 'blank', 'straight', 'face'],
        '😑': ['expressionless', 'blank', 'nothing'],
        '😶': ['speechless', 'no', 'mouth', 'blank'],
        '😏': ['smirk', 'sarcasm', 'witty', 'suggestive'],
        '😒': ['unamused', 'unimpressed', 'disapproval'],
        '🙄': ['roll', 'eyes', 'annoyed', 'whatever'],
        '😬': ['grimace', 'awkward', 'nervous', 'tense'],
        '🤥': ['lie', 'liar', 'pinocchio', 'dishonest'],
        '😌': ['relieved', 'content', 'relaxed', 'calm'],
        '😔': ['sad', 'pensive', 'disappointed', 'down'],
        '😪': ['sleepy', 'tired', 'drowsy', 'nap'],
        '🤤': ['drool', 'hungry', 'desire', 'want'],
        '😴': ['sleep', 'tired', 'snore', 'rest', 'dream', 'zzz'],
        '😷': ['mask', 'sick', 'ill', 'covid', 'doctor', 'medical'],
        '🤒': ['sick', 'fever', 'thermometer', 'ill'],
        '🤕': ['hurt', 'injured', 'bandage', 'pain'],
        '🤢': ['nausea', 'sick', 'green', 'vomit'],
        '🤮': ['vomit', 'sick', 'throw', 'up', 'ill'],
        '🤧': ['sneeze', 'sick', 'cold', 'allergy'],
        '🥵': ['hot', 'heat', 'sweat', 'summer', 'warm'],
        '🥶': ['cold', 'freezing', 'ice', 'winter', 'chill'],
        '🥴': ['woozy', 'dizzy', 'intoxicated', 'confused'],
        '😵': ['dizzy', 'confused', 'spiral', 'dead'],
        '🤯': ['explode', 'mind', 'blown', 'shocked', 'surprise'],
        '🤠': ['cowboy', 'hat', 'western', 'rodeo'],
        '🥳': ['party', 'celebration', 'birthday', 'happy'],
        '😎': ['cool', 'sunglasses', 'awesome', 'rad'],
        '🤓': ['nerd', 'geek', 'glasses', 'smart'],
        '🧐': ['monocle', 'sophisticated', 'examine', 'curious'],
        '😕': ['confused', 'puzzled', 'unsure', 'uncertainty'],
        '😟': ['worried', 'concerned', 'nervous', 'anxious'],
        '🙁': ['sad', 'frown', 'unhappy', 'disappointed'],
        '☹️': ['sad', 'frown', 'unhappy', 'down'],
        '😮': ['wow', 'surprise', 'shock', 'open', 'mouth'],
        '😯': ['hushed', 'surprised', 'shocked', 'wow'],
        '😲': ['astonished', 'shocked', 'amazed', 'surprised'],
        '😳': ['flushed', 'blush', 'shy', 'embarrassed'],
        '🥺': ['pleading', 'puppy', 'eyes', 'cute'],
        '😦': ['frown', 'open', 'mouth', 'surprise', 'shock'],
        '😧': ['anguish', 'shock', 'surprised', 'scared'],
        '😨': ['fear', 'scared', 'terrified', 'afraid'],
        '😰': ['anxious', 'sweat', 'nervous', 'worried'],
        '😥': ['sad', 'disappointed', 'relieved', 'sweat'],
        '😢': ['cry', 'sad', 'tear', 'unhappy'],
        '😭': ['sob', 'cry', 'tears', 'sad', 'wail'],
        '😱': ['scream', 'fear', 'scared', 'terror', 'shock'],
        '😖': ['confounded', 'confused', 'frustrated', 'anxious'],
        '😣': ['persevere', 'struggle', 'stressed', 'effort'],
        '😞': ['disappointed', 'sad', 'upset', 'unhappy'],
        '😓': ['sweat', 'stress', 'hot', 'tired'],
        '😩': ['weary', 'tired', 'exhausted', 'frustration'],
        '😫': ['tired', 'exhausted', 'stress', 'weary'],
        '🥱': ['yawn', 'sleepy', 'tired', 'bored'],
        '😤': ['triumph', 'pride', 'steam', 'determined'],
        '😡': ['angry', 'mad', 'rage', 'hate'],
        '😠': ['angry', 'mad', 'annoyed', 'frustrated'],
        '🤬': ['curse', 'swear', 'angry', 'outrage', 'symbols'],
        '😈': ['devil', 'evil', 'horns', 'bad', 'mischief'],
        '👿': ['imp', 'devil', 'angry', 'evil', 'bad'],
        '💀': ['skull', 'death', 'dead', 'danger', 'poison'],
        '☠️': ['skull', 'crossbones', 'death', 'danger', 'poison'],
        '💩': ['poop', 'pile', 'crap', 'shit', 'funny'],
        '🤡': ['clown', 'circus', 'joke', 'funny', 'scary'],
        '👹': ['ogre', 'monster', 'japanese', 'demon', 'oni'],
        '👺': ['goblin', 'monster', 'japanese', 'demon', 'tengu'],
        '👻': ['ghost', 'spooky', 'halloween', 'scary', 'boo'],
        '👽': ['alien', 'ufo', 'space', 'extraterrestrial'],
        '👾': ['alien', 'monster', 'game', 'space', 'invader'],
        '🤖': ['robot', 'machine', 'ai', 'automated'],
        '🎃': ['pumpkin', 'halloween', 'jack-o-lantern'],
        '😺': ['cat', 'happy', 'smile', 'pet', 'animal'],
        '😸': ['cat', 'grin', 'happy', 'pet', 'animal'],
        '😹': ['cat', 'joy', 'tears', 'laugh', 'happy'],
        '😻': ['cat', 'heart', 'eyes', 'love', 'pet'],
        '😼': ['cat', 'smirk', 'mischief', 'pet', 'animal'],
        '😽': ['cat', 'kiss', 'love', 'pet', 'animal'],
        '🙀': ['cat', 'surprised', 'wow', 'shock', 'fear'],
        '😿': ['cat', 'cry', 'sad', 'tear', 'unhappy'],
        '😾': ['cat', 'grumpy', 'angry', 'mad', 'annoyed'],
        '🙈': ['monkey', 'see', 'evil', 'cover', 'eyes'],
        '🙉': ['monkey', 'hear', 'evil', 'cover', 'ears'],
        '🙊': ['monkey', 'speak', 'evil', 'cover', 'mouth'],
        '💋': ['kiss', 'lips', 'love', 'romance'],
        '💌': ['love', 'letter', 'heart', 'mail', 'romance'],
        '💘': ['heart', 'arrow', 'cupid', 'love', 'romance'],
        '💝': ['heart', 'ribbon', 'gift', 'love', 'present'],
        '💖': ['heart', 'sparkle', 'love', 'excited'],
        '💗': ['heart', 'growing', 'excited', 'nervous', 'love'],
        '💓': ['heart', 'beating', 'pulse', 'love', 'emotion'],
        '💞': ['heart', 'revolving', 'love', 'relationship'],
        '💕': ['hearts', 'love', 'like', 'affection'],
        '💟': ['heart', 'decoration', 'love', 'purple'],
        '❣️': ['heart', 'exclamation', 'love', 'decoration'],
        '💔': ['heart', 'broken', 'sad', 'breakup', 'grief'],
        '❤️': ['heart', 'love', 'like', 'red', 'romance'],
        '🧡': ['heart', 'orange', 'love', 'like'],
        '💛': ['heart', 'yellow', 'love', 'like', 'friendship'],
        '💚': ['heart', 'green', 'love', 'like', 'nature'],
        '💙': ['heart', 'blue', 'love', 'like', 'calm'],
        '💜': ['heart', 'purple', 'love', 'like', 'royalty'],
        '🤎': ['heart', 'brown', 'love', 'like'],
        '🖤': ['heart', 'black', 'dark', 'love', 'like'],
        '🤍': ['heart', 'white', 'love', 'like', 'pure'],
        '💯': ['hundred', '100', 'score', 'perfect'],
        '💢': ['anger', 'mad', 'symbol', 'annoyed'],
        '💥': ['collision', 'explode', 'impact', 'boom'],
        '💫': ['dizzy', 'star', 'sparkle', 'swirl'],
        '💦': ['sweat', 'drops', 'water', 'splash'],
        '💨': ['dash', 'wind', 'air', 'fast', 'run'],
        '🕳️': ['hole', 'empty', 'dark', 'depression'],
        '💣': ['bomb', 'explode', 'boom', 'explosion'],
        '💬': ['speech', 'balloon', 'bubble', 'talk', 'chat'],
        '👁️‍🗨️': ['eye', 'speech', 'bubble', 'witness'],
        '🗨️': ['speech', 'bubble', 'chat', 'left', 'talk'],
        '🗯️': ['anger', 'bubble', 'mad', 'speech', 'right'],
        '💭': ['thought', 'bubble', 'dream', 'thinking'],
        '💤': ['sleep', 'zzz', 'tired', 'snooze'],
        '👋': ['wave', 'hello', 'goodbye', 'hand'],
        '🤚': ['hand', 'raised', 'high', 'five', 'stop'],
        '🖐️': ['hand', 'fingers', 'spread', 'stop', 'high'],
        '✋': ['hand', 'raised', 'high', 'five', 'stop'],
        '🖖': ['vulcan', 'spock', 'star', 'trek', 'live', 'long'],
        '👌': ['ok', 'hand', 'perfect', 'agree'],
        '🤌': ['pinched', 'fingers', 'italian', 'what'],
        '🤏': ['pinch', 'small', 'little', 'tiny', 'amount'],
        '✌️': ['peace', 'victory', 'fingers', 'hand'],
        '🤞': ['fingers', 'crossed', 'luck', 'hopeful'],
        '🤟': ['love', 'you', 'hand', 'gesture', 'ily'],
        '🤘': ['rock', 'on', 'hand', 'horns', 'metal', 'music'],
        '🤙': ['call', 'me', 'hand', 'phone', 'gesture'],
        '👈': ['point', 'left', 'hand', 'direction', 'finger'],
        '👉': ['point', 'right', 'hand', 'direction', 'finger'],
        '👆': ['point', 'up', 'hand', 'direction', 'finger'],
        '🖕': ['middle', 'finger', 'rude', 'offensive'],
        '👇': ['point', 'down', 'hand', 'direction', 'finger'],
        '☝️': ['point', 'up', 'hand', 'direction', 'finger'],
        '👍': ['thumbs', 'up', 'approve', 'like', 'yes'],
        '👎': ['thumbs', 'down', 'disapprove', 'dislike', 'no'],
        '✊': ['fist', 'hand', 'punch', 'power', 'protest'],
        '👊': ['fist', 'bump', 'punch', 'hand', 'hit'],
        '🤛': ['fist', 'left', 'punch', 'bump', 'hand'],
        '🤜': ['fist', 'right', 'punch', 'bump', 'hand'],
        '👏': ['clap', 'applause', 'hands', 'praise'],
        '🙌': ['raise', 'hands', 'celebration', 'praise', 'hooray'],
        '👐': ['open', 'hands', 'hug', 'jazz', 'welcome'],
        '🤲': ['palms', 'up', 'together', 'pray', 'beg'],
        '🤝': ['handshake', 'agreement', 'deal', 'meeting'],
        '🙏': ['pray', 'please', 'hope', 'wish', 'thank'],
        '✍️': ['write', 'hand', 'pen', 'writing', 'signature'],
        '💅': ['nail', 'polish', 'manicure', 'cosmetics', 'beauty'],
        '🤳': ['selfie', 'camera', 'phone', 'photo'],
        '💪': ['muscle', 'biceps', 'strong', 'strength', 'flex'],
        '🦾': ['mechanical', 'arm', 'robot', 'prosthetic'],
        '🦿': ['mechanical', 'leg', 'robot', 'prosthetic'],
        '🦵': ['leg', 'kick', 'foot', 'limb'],
        '🦶': ['foot', 'toe', 'kick', 'stomp'],
        '👂': ['ear', 'hear', 'listen', 'sound'],
        '🦻': ['ear', 'hearing', 'aid', 'deaf', 'accessibility'],
        '👃': ['nose', 'smell', 'sniff', 'face'],
        '🧠': ['brain', 'smart', 'intelligent', 'mind'],
        '🫀': ['heart', 'organ', 'anatomical', 'cardiac'],
        '🫁': ['lungs', 'breath', 'organ', 'respiratory'],
        '🦷': ['tooth', 'teeth', 'dentist', 'mouth'],
        '🦴': ['bone', 'skeleton', 'joint', 'structure'],
        '👀': ['eyes', 'look', 'see', 'watch', 'stare'],
        '👁️': ['eye', 'look', 'see', 'watch', 'vision'],
        '👅': ['tongue', 'taste', 'lick', 'mouth'],
        '👄': ['mouth', 'lips', 'kiss', 'speak']
    };

    // Return emoji keywords if available, or just the emoji itself as a keyword
    return emojiKeywords[emoji] || [emoji];
}

/**
 * Focuses the search input in the emoji picker
 */
function focusEmojiSearch(): void {
    const searchInput = document.getElementById('emoji-search');
    if (searchInput) {
        searchInput.focus();
    }
}

/**
 * Adds or removes the 'focused' class from the search input
 * @param focused Whether the input should be focused
 */
function toggleEmojiSearchFocus(focused: boolean): void {
    const searchInput = document.getElementById('emoji-search');
    if (searchInput) {
        if (focused) {
            searchInput.classList.add('focused');
        } else {
            searchInput.classList.remove('focused');
        }
    }
}

/**
 * Filters emojis based on search query with optimized performance
 * @param query - The search query
 */
function filterEmojis(query: string): void {
    // 検索キャッシュ
    const cache: Record<string, boolean> = {};
    
    // 入力欄の値を更新
    const searchInput = document.getElementById('emoji-search') as HTMLInputElement;
    if (searchInput) {
        searchInput.value = query;
    }

    const allButtons = document.querySelectorAll<HTMLElement>('.emoji-picker-button');
    const allCategories = document.querySelectorAll<HTMLElement>('.emoji-category');
    
    // 検索クエリが空の場合はすべて表示
    if (!query || query.trim() === '') {
        // DOM更新を最小限に抑えるために一時停止
        requestAnimationFrame(() => {
            allButtons.forEach(button => {
                button.style.display = 'inline-block';
            });
            
            allCategories.forEach(category => {
                category.style.display = 'block';
            });
            
            // 検索結果なしメッセージを削除
            const noResults = document.getElementById('emoji-no-results');
            if (noResults) {
                noResults.remove();
            }
        });
        return;
    }

    const searchTerm = query.toLowerCase().trim();
    let visibleCount = 0;
    let visibleCategories = new Set<HTMLElement>();

    // バッチ処理を最適化
    const processBatch = (buttons: NodeListOf<HTMLElement>, startIndex: number, batchSize: number): Promise<void> => {
        return new Promise(resolve => {
            // 非同期で処理を行うことでUIスレッドをブロックしない
            setTimeout(() => {
                const endIndex = Math.min(startIndex + batchSize, buttons.length);
                
                for (let i = startIndex; i < endIndex; i++) {
                    const button = buttons[i];
                    const emoji = button.textContent?.trim() || '';
                    
                    // キャッシュを使用して重複計算を避ける
                    if (cache[emoji] === undefined) {
                        const keywords = getEmojiKeywords(emoji);
                        cache[emoji] = keywords.some(keyword => 
                            keyword.toLowerCase().includes(searchTerm)
                        );
                    }
                    
                    if (cache[emoji]) {
                        button.style.display = 'inline-block';
                        // カテゴリを表示
                        const category = button.closest('.emoji-category') as HTMLElement;
                        if (category) {
                            visibleCategories.add(category);
                        }
                        visibleCount++;
                    } else {
                        button.style.display = 'none';
                    }
                }
                
                // さらに処理が必要な場合は続行
                if (endIndex < buttons.length) {
                    processBatch(buttons, endIndex, batchSize).then(resolve);
                } else {
                    // すべての処理が完了したら結果を表示
                    requestAnimationFrame(() => {
                        // カテゴリの表示/非表示を一括で更新
                        allCategories.forEach(category => {
                            category.style.display = visibleCategories.has(category) ? 'block' : 'none';
                        });
                        
                        // 結果がない場合はメッセージを表示
                        if (visibleCount === 0) {
                            const noResults = document.getElementById('emoji-no-results');
                            if (!noResults) {
                                const message = document.createElement('div');
                                message.id = 'emoji-no-results';
                                message.className = 'emoji-no-results';
                                message.textContent = 'No emojis found for "' + query + '"';
                                
                                const container = document.querySelector('.emoji-picker-content');
                                if (container) {
                                    container.appendChild(message);
                                }
                            }
                        } else {
                            // 結果がある場合はメッセージを削除
                            const noResults = document.getElementById('emoji-no-results');
                            if (noResults) {
                                noResults.remove();
                            }
                        }
                        resolve();
                    });
                }
            }, 0);
        });
    };
    
    // 処理開始前にカテゴリを非表示にする
    requestAnimationFrame(() => {
        allCategories.forEach(category => {
            category.style.display = 'none';
        });
        
        // バッチサイズを調整（短いクエリの場合は大きく、長いクエリの場合は小さく）
        const batchSize = searchTerm.length <= 2 ? 30 : 50;
        processBatch(allButtons, 0, batchSize);
    });
}

// Variables for emoji picker modal
let currentEmojiResolve: ((value: string) => void) | null = null;

// イベントハンドラーを関数として定義（スコープ外からも参照できるように）
function handleCloseBtnMouseover(e: Event): void {
    const target = e.target as HTMLElement;
    if (target) {
        target.style.backgroundColor = '#e9ecef';
    }
}

function handleCloseBtnMouseout(e: Event): void {
    const target = e.target as HTMLElement;
    if (target) {
        target.style.backgroundColor = 'transparent';
    }
}

function handleCloseBtnClick(): void {
    closeEmojiPicker('');
}

function handleBackdropClick(e: MouseEvent): void {
    const backdrop = document.getElementById('emoji-picker-backdrop');
    if (e.target === backdrop) {
        closeEmojiPicker('');
    }
}

function handleEscapeKey(e: KeyboardEvent): void {
    if (e.key === 'Escape') {
        closeEmojiPicker('');
        document.removeEventListener('keydown', handleEscapeKey);
    }
}

/**
 * Shows the emoji picker modal and returns the selected emoji
 * @param emojiHtml - The HTML content for the emoji picker
 * @returns Promise for the selected emoji or empty string if cancelled
 */
function showEmojiPicker(emojiHtml: string): Promise<string> {
    return new Promise((resolve) => {
        currentEmojiResolve = resolve;
        
        // Create modal backdrop
        const backdrop = document.createElement('div');
        backdrop.id = 'emoji-picker-backdrop';
        backdrop.className = 'emoji-picker-backdrop';
        
        // Create modal dialog
        const modal = document.createElement('div');
        modal.id = 'emoji-picker-modal';
        modal.className = 'emoji-picker-modal';
        
        // Create header with close button
        const header = document.createElement('div');
        header.classList.add('emoji-modal-dynamic-header');

        const title = document.createElement('h4');
        title.textContent = 'Select Emoji';
        title.classList.add('emoji-modal-dynamic-title');
        
        const closeBtn = document.createElement('button');
        closeBtn.innerHTML = '&times;';
        closeBtn.classList.add('emoji-modal-dynamic-close');
        
        // 関数を参照するようにイベントリスナーを設定
        closeBtn.addEventListener('mouseover', handleCloseBtnMouseover);
        closeBtn.addEventListener('mouseout', handleCloseBtnMouseout);
        closeBtn.addEventListener('click', handleCloseBtnClick);

        header.appendChild(title);
        header.appendChild(closeBtn);

        // Create content area
        const content = document.createElement('div');
        content.innerHTML = emojiHtml;

        modal.appendChild(header);
        modal.appendChild(content);
        backdrop.appendChild(modal);

        // 関数を参照するように設定
        backdrop.addEventListener('click', handleBackdropClick);

        // Escape キーのイベントリスナー
        document.addEventListener('keydown', handleEscapeKey);

        document.body.appendChild(backdrop);
        
        // Initialize search input with a slight delay to ensure the DOM is ready
        setTimeout(() => {
            const searchInput = document.getElementById('emoji-search');
            if (searchInput) {
                searchInput.focus();
            }
        }, 100);

        // Focus the modal for keyboard navigation
        modal.tabIndex = -1;
        modal.focus();
    });
}

/**
 * Handles emoji selection from the picker
 * @param emoji - The selected emoji
 */
function selectEmoji(emoji: string): void {
    closeEmojiPicker(emoji);
}

/**
 * Closes the emoji picker and resolves the promise
 * @param emoji - The selected emoji or empty string if cancelled
 */
function closeEmojiPicker(emoji: string): void {
    const backdrop = document.getElementById('emoji-picker-backdrop');
    if (backdrop) {
        // イベントリスナーのクリーンアップを改善
        try {
            // クリックイベントのリスナーを削除
            backdrop.removeEventListener('click', handleBackdropClick);
            
            // 全てのボタンからイベントリスナーを削除
            const closeButton = backdrop.querySelector('.emoji-modal-dynamic-close');
            if (closeButton) {
                // TypeScriptの型変換を正しく処理
                const closeBtn = closeButton as HTMLElement;
                closeBtn.removeEventListener('mouseover', handleCloseBtnMouseover);
                closeBtn.removeEventListener('mouseout', handleCloseBtnMouseout);
                closeBtn.removeEventListener('click', handleCloseBtnClick);
            }
            
            // Escapeキーのイベントリスナーを削除
            document.removeEventListener('keydown', handleEscapeKey);
        } catch (e) {
            console.error('Error cleaning up event handlers:', e);
        }
        
        backdrop.remove();
    }
    
    if (currentEmojiResolve) {
        currentEmojiResolve(emoji);
        currentEmojiResolve = null;
    }
}

// Assign functions to window object for global access
window.getEmojiKeywords = getEmojiKeywords;
window.focusEmojiSearch = focusEmojiSearch;
window.toggleEmojiSearchFocus = toggleEmojiSearchFocus;
window.filterEmojis = filterEmojis;
window.showEmojiPicker = showEmojiPicker;
window.selectEmoji = selectEmoji;

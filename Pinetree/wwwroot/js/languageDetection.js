// Browser language detection utility
window.detectBrowserLanguage = function() {
    try {
        return navigator?.language || navigator?.languages?.[0] || null;
    } catch {
        return null;
    }
};

// Debug function to show all available language information
window.debugLanguageInfo = function() {
    console.log('=== Language Detection Debug Info ===');
    console.log('navigator object:', navigator);
    console.log('navigator.language:', navigator?.language);
    console.log('navigator.languages:', navigator?.languages);
    console.log('navigator.userLanguage:', navigator?.userLanguage);
    console.log('navigator.browserLanguage:', navigator?.browserLanguage);
    console.log('document.documentElement.lang:', document?.documentElement?.lang);
    console.log('Detected language:', window.detectBrowserLanguage());
    console.log('=====================================');
};

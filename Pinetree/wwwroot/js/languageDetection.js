// Browser language detection utility
window.detectBrowserLanguage = function() {
    try {
        // Try multiple approaches to detect browser language
        let language = null;
        
        // Method 1: navigator.language
        if (navigator && navigator.language) {
            language = navigator.language;
            console.log('Method 1 (navigator.language):', language);
            return language;
        }
        
        // Method 2: navigator.languages array
        if (navigator && navigator.languages && navigator.languages.length > 0) {
            language = navigator.languages[0];
            console.log('Method 2 (navigator.languages[0]):', language);
            return language;
        }
        
        // Method 3: navigator.userLanguage (IE)
        if (navigator && navigator.userLanguage) {
            language = navigator.userLanguage;
            console.log('Method 3 (navigator.userLanguage):', language);
            return language;
        }
        
        // Method 4: navigator.browserLanguage (IE)
        if (navigator && navigator.browserLanguage) {
            language = navigator.browserLanguage;
            console.log('Method 4 (navigator.browserLanguage):', language);
            return language;
        }
        
        // Method 5: document.documentElement.lang
        if (document && document.documentElement && document.documentElement.lang) {
            language = document.documentElement.lang;
            console.log('Method 5 (document.documentElement.lang):', language);
            return language;
        }
        
        console.log('No browser language detected, all methods failed');
        return null;
        
    } catch (error) {
        console.error('Error detecting browser language:', error);
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

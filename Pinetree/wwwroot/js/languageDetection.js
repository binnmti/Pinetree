window.detectBrowserLanguage = function() {
    try {
        return navigator?.language || navigator?.languages?.[0] || null;
    } catch {
        return null;
    }
};

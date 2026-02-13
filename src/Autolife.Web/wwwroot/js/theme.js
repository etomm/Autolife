// Theme detection helper
window.matchMedia = function(query) {
    if (query === '(prefers-color-scheme: dark)') {
        return window.matchMedia(query).matches;
    }
    return false;
};

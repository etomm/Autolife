// Breadcrumb measurement utilities
window.breadcrumbMeasure = {
    getContainerWidth: function() {
        const container = document.querySelector('.breadcrumb-nav:not(.hidden)');
        return container ? container.offsetWidth : 0;
    },
    
    getContentWidth: function() {
        const container = document.querySelector('.breadcrumb-nav.hidden .breadcrumb-content');
        return container ? container.scrollWidth : 0;
    },
    
    getSegmentWidth: function(index) {
        const segment = document.querySelector('.breadcrumb-nav.hidden [data-measure-segment="' + index + '"]');
        if (!segment) return 0;
        
        // Find preceding separator
        const prev = segment.previousElementSibling;
        let sepWidth = 0;
        if (prev && prev.hasAttribute('data-measure-sep')) {
            sepWidth = prev.offsetWidth;
        }
        
        return segment.offsetWidth + sepWidth;
    },
    
    log: function(message) {
        console.log('[BREADCRUMB] ' + message);
    }
};

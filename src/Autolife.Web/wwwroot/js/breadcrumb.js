// Client-side breadcrumb management with double-buffering
window.breadcrumbManager = {
    primaryVisible: true,
    
    // Initialize breadcrumbs with path data
    updateBreadcrumb: function(pathData) {
        console.log('[BREADCRUMB] Updating breadcrumb with', pathData);
        
        // pathData = { rootPrefix: "C:", segments: [{label: "Users", path: "C:\\Users"}, ...] }
        
        // Get both breadcrumb containers
        const primary = document.querySelector('.breadcrumb-nav[data-buffer="primary"]');
        const secondary = document.querySelector('.breadcrumb-nav[data-buffer="secondary"]');
        
        if (!primary || !secondary) {
            console.error('[BREADCRUMB] Could not find breadcrumb containers');
            return;
        }
        
        // Determine which is currently visible and which is hidden
        const visible = this.primaryVisible ? primary : secondary;
        const hidden = this.primaryVisible ? secondary : primary;
        
        console.log('[BREADCRUMB] Primary visible:', this.primaryVisible);
        console.log('[BREADCRUMB] Populating hidden breadcrumb...');
        
        // Populate the HIDDEN breadcrumb with new data
        this.populateBreadcrumb(hidden, pathData);
        
        // Wait for render, then measure and calculate
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                this.calculateAndSwap(visible, hidden, pathData);
            });
        });
    },
    
    populateBreadcrumb: function(container, pathData) {
        const content = container.querySelector('.breadcrumb-content');
        if (!content) return;
        
        content.innerHTML = '';
        
        // Add root prefix button
        if (pathData.rootPrefix) {
            const rootBtn = document.createElement('button');
            rootBtn.type = 'button';
            rootBtn.className = 'breadcrumb-item';
            rootBtn.textContent = pathData.rootPrefix;
            rootBtn.setAttribute('data-path', pathData.rootPath || pathData.rootPrefix);
            content.appendChild(rootBtn);
        }
        
        // Add all segments
        pathData.segments.forEach((seg, index) => {
            // Add separator
            const sep = document.createElement('span');
            sep.className = 'breadcrumb-separator';
            sep.textContent = '/';
            sep.setAttribute('data-measure-sep', '');
            content.appendChild(sep);
            
            // Add segment button
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'breadcrumb-item';
            if (index === pathData.segments.length - 1) {
                btn.classList.add('breadcrumb-current');
            }
            btn.textContent = seg.label;
            btn.setAttribute('data-path', seg.path);
            btn.setAttribute('data-measure-segment', index);
            content.appendChild(btn);
        });
    },
    
    calculateAndSwap: function(visible, hidden, pathData) {
        console.log('[BREADCRUMB] === CALCULATION START ===');
        
        // Measure container width from visible breadcrumb
        const containerWidth = visible.offsetWidth;
        console.log('[BREADCRUMB] Container width:', containerWidth + 'px');
        
        // Measure content width from hidden breadcrumb (using scrollWidth on content)
        const hiddenContent = hidden.querySelector('.breadcrumb-content');
        const contentWidth = hiddenContent ? hiddenContent.scrollWidth : 0;
        console.log('[BREADCRUMB] Content width:', contentWidth + 'px');
        
        if (containerWidth <= 0 || contentWidth <= 0) {
            console.warn('[BREADCRUMB] Invalid measurements, showing everything');
            this.swap(visible, hidden);
            return;
        }
        
        const overflow = contentWidth - containerWidth;
        console.log('[BREADCRUMB] Overflow:', overflow + 'px');
        
        if (overflow <= 10) {
            console.log('[BREADCRUMB] ✅ Everything fits, no ellipsis needed');
            this.swap(visible, hidden);
            return;
        }
        
        console.log('[BREADCRUMB] ❌ Need ellipsis, calculating...');
        
        // Measure individual segments (excluding last one which is always shown)
        const segments = hiddenContent.querySelectorAll('[data-measure-segment]');
        const segmentCount = segments.length;
        
        if (segmentCount <= 1) {
            console.log('[BREADCRUMB] Only 1 segment, showing anyway');
            this.swap(visible, hidden);
            return;
        }
        
        const segmentWidths = [];
        for (let i = 0; i < segmentCount - 1; i++) {
            const seg = segments[i];
            const prev = seg.previousElementSibling;
            let width = seg.offsetWidth;
            
            // Add separator width if present
            if (prev && prev.hasAttribute('data-measure-sep')) {
                width += prev.offsetWidth;
            }
            
            segmentWidths.push(width);
            console.log('[BREADCRUMB]   Segment', i, '"' + seg.textContent + '":', width + 'px');
        }
        
        // Calculate how many to keep
        const ellipsisWidth = 50; // Approximate
        const targetRemoval = overflow + ellipsisWidth;
        let accumulated = 0;
        let keepCount = 0;
        
        console.log('[BREADCRUMB] Target removal:', targetRemoval + 'px');
        
        for (let i = 0; i < segmentWidths.length; i++) {
            if (accumulated < targetRemoval) {
                accumulated += segmentWidths[i];
                console.log('[BREADCRUMB]   Removing segment', i, ', accumulated:', accumulated + 'px');
            } else {
                keepCount = i;
                console.log('[BREADCRUMB]   Stopping at', i, ', keeping', keepCount, 'segments');
                break;
            }
        }
        
        console.log('[BREADCRUMB] ✂️ Final: keep', keepCount, 'segments, add ellipsis');
        
        // Apply ellipsis by hiding segments
        this.applyEllipsis(hiddenContent, keepCount, pathData.segments.length);
        
        // Swap visibility
        this.swap(visible, hidden);
    },
    
    applyEllipsis: function(content, keepCount, totalSegments) {
        const segments = content.querySelectorAll('[data-measure-segment]');
        
        // Hide segments from keepCount to (totalSegments - 2)
        for (let i = 0; i < segments.length - 1; i++) {
            const seg = segments[i];
            const sep = seg.previousElementSibling;
            
            if (i < keepCount) {
                // Keep visible
                seg.style.display = '';
                if (sep) sep.style.display = '';
            } else {
                // Hide
                seg.style.display = 'none';
                if (sep) sep.style.display = 'none';
            }
        }
        
        // Add ellipsis button after kept segments
        const existingEllipsis = content.querySelector('.breadcrumb-ellipsis');
        if (existingEllipsis) {
            existingEllipsis.remove();
        }
        
        const lastSegment = segments[segments.length - 1];
        
        // Create ellipsis
        const ellipsisSep = document.createElement('span');
        ellipsisSep.className = 'breadcrumb-separator';
        ellipsisSep.textContent = '/';
        
        const ellipsisBtn = document.createElement('button');
        ellipsisBtn.type = 'button';
        ellipsisBtn.className = 'breadcrumb-ellipsis';
        ellipsisBtn.innerHTML = '<span>..</span>';
        ellipsisBtn.title = 'Go up one level';
        
        // Insert before last segment
        const lastSep = lastSegment.previousElementSibling;
        if (lastSep) {
            content.insertBefore(ellipsisBtn, lastSep);
            content.insertBefore(ellipsisSep, lastSep);
        }
    },
    
    swap: function(visible, hidden) {
        console.log('[BREADCRUMB] 🔄 Swapping breadcrumbs...');
        
        // Swap visibility using visibility property (not display)
        visible.style.visibility = 'hidden';
        visible.style.position = 'absolute';
        
        hidden.style.visibility = 'visible';
        hidden.style.position = 'relative';
        
        // Toggle tracking
        this.primaryVisible = !this.primaryVisible;
        
        console.log('[BREADCRUMB] Primary visible now:', this.primaryVisible);
        console.log('[BREADCRUMB] === CALCULATION END ===');
    },
    
    log: function(message) {
        console.log('[BREADCRUMB] ' + message);
    }
};

// Legacy measurement functions (keep for compatibility)
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

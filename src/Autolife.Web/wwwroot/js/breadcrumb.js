// breadcrumb.js - Client-side breadcrumb manager with double buffering

window.breadcrumbManager = {
    currentBuffer: 'primary',
    dotNetHelper: null,
    
    init: function(dotNetHelper) {
        this.dotNetHelper = dotNetHelper;
        console.log('[BREADCRUMB] Manager initialized');
    },
    
    updateBreadcrumb: function(pathData) {
        console.log('[BREADCRUMB] === UPDATE START ===');
        console.log('[BREADCRUMB] Path data:', pathData);
        
        // Get the stable container (always visible) and both content divs
        const container = document.querySelector('.breadcrumb-nav');
        const primaryContent = container?.querySelector('.breadcrumb-content[data-buffer="primary"]');
        const secondaryContent = container?.querySelector('.breadcrumb-content[data-buffer="secondary"]');
        
        if (!container || !primaryContent || !secondaryContent) {
            console.error('[BREADCRUMB] ❌ Required elements not found');
            return;
        }
        
        // Determine which buffer to use (opposite of current visible one)
        const hiddenBuffer = this.currentBuffer === 'primary' ? 'secondary' : 'primary';
        const hiddenContent = hiddenBuffer === 'primary' ? primaryContent : secondaryContent;
        
        console.log(`[BREADCRUMB] Current visible: ${this.currentBuffer}, updating: ${hiddenBuffer}`);
        
        // Build HTML in the hidden buffer
        const html = this.buildBreadcrumbHTML(pathData);
        hiddenContent.innerHTML = html;
        
        // Make hidden buffer measurable but invisible
        hiddenContent.style.visibility = 'hidden';
        hiddenContent.style.position = 'absolute';
        hiddenContent.style.top = '0';
        hiddenContent.style.left = '0';
        hiddenContent.style.right = '0';
        hiddenContent.style.pointerEvents = 'none';
        
        // Wait for render
        requestAnimationFrame(() => {
            this.calculateAndApplyEllipsis(container, hiddenContent, hiddenBuffer, pathData);
        });
    },
    
    buildBreadcrumbHTML: function(pathData) {
        let html = '';
        
        // Root prefix (e.g., "C:", "/", "Network")
        if (pathData.rootPrefix) {
            html += `<button type="button" class="breadcrumb-item" data-navigate="${this.escapeHtml(pathData.rootPath || pathData.rootPrefix)}">`;
            html += this.escapeHtml(pathData.rootPrefix);
            html += `</button>`;
        }
        
        // Path segments
        if (pathData.segments && pathData.segments.length > 0) {
            pathData.segments.forEach((segment, index) => {
                html += `<span class="breadcrumb-separator">/</span>`;
                
                const isLast = (index === pathData.segments.length - 1);
                if (isLast) {
                    html += `<button type="button" class="breadcrumb-item breadcrumb-current" data-segment-index="${index}">`;
                } else {
                    html += `<button type="button" class="breadcrumb-item" data-segment-index="${index}" data-navigate="${this.escapeHtml(segment.path)}">`;
                }
                html += this.escapeHtml(segment.label);
                html += `</button>`;
            });
        }
        
        return html;
    },
    
    calculateAndApplyEllipsis: function(container, content, bufferName, pathData) {
        console.log('[BREADCRUMB] === CALCULATION START ===');
        
        const containerWidth = container.offsetWidth;
        const contentWidth = content.scrollWidth;
        
        console.log(`[BREADCRUMB] Container width: ${containerWidth}px`);
        console.log(`[BREADCRUMB] Content width: ${contentWidth}px`);
        
        if (contentWidth === 0) {
            console.warn('[BREADCRUMB] ⚠️ Content width is 0, aborting calculation');
            return;
        }
        
        const overflow = contentWidth - containerWidth;
        console.log(`[BREADCRUMB] Overflow: ${overflow}px`);
        
        if (overflow <= 0) {
            console.log('[BREADCRUMB] ✅ No overflow, showing everything');
            this.swapBuffers(bufferName);
            this.attachNavigationHandlers();
            return;
        }
        
        console.log('[BREADCRUMB] ❌ Need ellipsis, calculating...');
        
        // Get all segment buttons (excluding root and current)
        const segments = Array.from(content.querySelectorAll('.breadcrumb-item[data-segment-index]'));
        const middleSegments = segments.slice(0, -1); // Exclude last (current)
        
        if (middleSegments.length === 0) {
            console.log('[BREADCRUMB] ⚠️ No middle segments to collapse');
            this.swapBuffers(bufferName);
            this.attachNavigationHandlers();
            return;
        }
        
        // Measure each middle segment
        const segmentWidths = [];
        middleSegments.forEach((btn, index) => {
            const width = btn.offsetWidth;
            segmentWidths.push(width);
            console.log(`[BREADCRUMB]   Segment ${index} "${btn.textContent}": ${width}px`);
        });
        
        // Calculate how much to remove (overflow + ellipsis width + buffer)
        const ellipsisWidth = 50; // Approximate width of ".." button
        const buffer = 20; // Safety margin
        const targetRemoval = overflow + ellipsisWidth + buffer;
        console.log(`[BREADCRUMB] Target removal: ${targetRemoval}px`);
        
        // Determine how many segments to remove from the middle
        let accumulated = 0;
        let removeCount = 0;
        for (let i = 0; i < segmentWidths.length; i++) {
            accumulated += segmentWidths[i] + 10; // +10 for separator
            removeCount = i + 1;
            console.log(`[BREADCRUMB]   Removing segment ${i}, accumulated: ${accumulated}px`);
            if (accumulated >= targetRemoval) {
                break;
            }
        }
        
        const keepCount = middleSegments.length - removeCount;
        console.log(`[BREADCRUMB] ✂️ Final: keep ${keepCount} segments, add ellipsis`);
        
        // Rebuild with ellipsis
        const htmlWithEllipsis = this.buildBreadcrumbWithEllipsis(pathData, keepCount);
        content.innerHTML = htmlWithEllipsis;
        
        this.swapBuffers(bufferName);
        this.attachNavigationHandlers();
    },
    
    buildBreadcrumbWithEllipsis: function(pathData, keepCount) {
        let html = '';
        
        // Root prefix
        if (pathData.rootPrefix) {
            html += `<button type="button" class="breadcrumb-item" data-navigate="${this.escapeHtml(pathData.rootPath || pathData.rootPrefix)}">`;
            html += this.escapeHtml(pathData.rootPrefix);
            html += `</button>`;
        }
        
        if (!pathData.segments || pathData.segments.length === 0) {
            return html;
        }
        
        // Leading segments (those we keep)
        for (let i = 0; i < keepCount; i++) {
            const segment = pathData.segments[i];
            html += `<span class="breadcrumb-separator">/</span>`;
            html += `<button type="button" class="breadcrumb-item" data-navigate="${this.escapeHtml(segment.path)}">`;
            html += this.escapeHtml(segment.label);
            html += `</button>`;
        }
        
        // Ellipsis
        html += `<span class="breadcrumb-separator">/</span>`;
        html += `<button type="button" class="breadcrumb-ellipsis" data-navigate-up="true">`;
        html += `<span>..</span>`;
        html += `</button>`;
        
        // Last segment (current)
        const lastSegment = pathData.segments[pathData.segments.length - 1];
        html += `<span class="breadcrumb-separator">/</span>`;
        html += `<button type="button" class="breadcrumb-item breadcrumb-current">`;
        html += this.escapeHtml(lastSegment.label);
        html += `</button>`;
        
        return html;
    },
    
    swapBuffers: function(newVisibleBuffer) {
        console.log('[BREADCRUMB] 🔄 Swapping content...');
        
        const container = document.querySelector('.breadcrumb-nav');
        const primaryContent = container?.querySelector('.breadcrumb-content[data-buffer="primary"]');
        const secondaryContent = container?.querySelector('.breadcrumb-content[data-buffer="secondary"]');
        
        if (!primaryContent || !secondaryContent) {
            console.error('[BREADCRUMB] ❌ Content divs not found for swap');
            return;
        }
        
        // Hide current, show new
        if (newVisibleBuffer === 'primary') {
            primaryContent.style.visibility = 'visible';
            primaryContent.style.position = 'relative';
            primaryContent.style.pointerEvents = 'auto';
            
            secondaryContent.style.visibility = 'hidden';
            secondaryContent.style.position = 'absolute';
            secondaryContent.style.top = '0';
            secondaryContent.style.left = '0';
            secondaryContent.style.right = '0';
            secondaryContent.style.pointerEvents = 'none';
        } else {
            secondaryContent.style.visibility = 'visible';
            secondaryContent.style.position = 'relative';
            secondaryContent.style.pointerEvents = 'auto';
            
            primaryContent.style.visibility = 'hidden';
            primaryContent.style.position = 'absolute';
            primaryContent.style.top = '0';
            primaryContent.style.left = '0';
            primaryContent.style.right = '0';
            primaryContent.style.pointerEvents = 'none';
        }
        
        this.currentBuffer = newVisibleBuffer;
        console.log(`[BREADCRUMB] Visible buffer now: ${this.currentBuffer}`);
        console.log('[BREADCRUMB] === CALCULATION END ===');
    },
    
    attachNavigationHandlers: function() {
        const container = document.querySelector('.breadcrumb-nav');
        if (!container) return;
        
        // Remove old handlers
        const oldHandlers = container.querySelectorAll('[data-navigate], [data-navigate-up]');
        oldHandlers.forEach(btn => {
            const newBtn = btn.cloneNode(true);
            btn.parentNode.replaceChild(newBtn, btn);
        });
        
        // Attach new handlers
        const navigateBtns = container.querySelectorAll('[data-navigate]');
        navigateBtns.forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.preventDefault();
                const path = btn.getAttribute('data-navigate');
                if (path && this.dotNetHelper) {
                    console.log(`[BREADCRUMB] Navigate to: ${path}`);
                    this.dotNetHelper.invokeMethodAsync('NavigateToPath', path);
                }
            });
        });
        
        const upBtn = container.querySelector('[data-navigate-up]');
        if (upBtn && this.dotNetHelper) {
            upBtn.addEventListener('click', (e) => {
                e.preventDefault();
                console.log('[BREADCRUMB] Navigate up one level');
                this.dotNetHelper.invokeMethodAsync('NavigateUpOneLevel');
            });
        }
    },
    
    escapeHtml: function(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
};

console.log('[BREADCRUMB] breadcrumb.js loaded');

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
        
        // Get the stable container and both content divs
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
        
        // Wait for render
        requestAnimationFrame(() => {
            this.showBuffer(hiddenBuffer, container.offsetWidth);
            this.attachNavigationHandlers();
        });
    },
    
    buildBreadcrumbHTML: function(pathData) {
        let html = '';
        
        if (!pathData.segments || pathData.segments.length === 0) {
            return html;
        }
        
        // Build all segments (root is now just segments[0])
        pathData.segments.forEach((segment, index) => {
            if (index > 0) {
                html += `<span class="breadcrumb-separator">/</span>`;
            }
            
            const isLast = (index === pathData.segments.length - 1);
            if (isLast) {
                html += `<button type="button" class="breadcrumb-item breadcrumb-current" data-segment-index="${index}">`;
            } else {
                html += `<button type="button" class="breadcrumb-item" data-segment-index="${index}" data-navigate="${this.escapeHtml(segment.path)}">`;
            }
            html += this.escapeHtml(segment.label);
            html += `</button>`;
        });
        
        return html;
    },
    
    showBuffer: function(bufferName, width) {
        console.log(`[BREADCRUMB] 🔄 Showing buffer: ${bufferName} with width: ${width}px`);
        
        const container = document.querySelector('.breadcrumb-nav');
        const primaryContent = container?.querySelector('.breadcrumb-content[data-buffer="primary"]');
        const secondaryContent = container?.querySelector('.breadcrumb-content[data-buffer="secondary"]');
        
        if (!primaryContent || !secondaryContent) {
            console.error('[BREADCRUMB] ❌ Content divs not found');
            return;
        }
        
        // Both stay absolute, just toggle visibility and set width
        if (bufferName === 'primary') {
            primaryContent.style.visibility = 'visible';
            primaryContent.style.width = width + 'px';
            primaryContent.style.pointerEvents = 'auto';
            
            secondaryContent.style.visibility = 'hidden';
            secondaryContent.style.pointerEvents = 'none';
        } else {
            secondaryContent.style.visibility = 'visible';
            secondaryContent.style.width = width + 'px';
            secondaryContent.style.pointerEvents = 'auto';
            
            primaryContent.style.visibility = 'hidden';
            primaryContent.style.pointerEvents = 'none';
        }
        
        this.currentBuffer = bufferName;
        console.log(`[BREADCRUMB] Visible buffer now: ${this.currentBuffer}`);
        console.log('[BREADCRUMB] === UPDATE END ===');
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

window.initResizers = function () {
    const resizer1 = document.getElementById('resizer1');
    const resizer2 = document.getElementById('resizer2');
    const panel1 = document.getElementById('panel1');
    const panel2 = document.getElementById('panel2');
      if (!resizer1 || !panel1) {
        return;
    }
    
    function setupResizer(resizer, targetPanel, minPercent, maxPercent) {
        let startX, startWidth, parentWidth;
        
        function onMouseDown(e) {
            try {
                e.preventDefault();
                e.stopPropagation();
                
                if (!targetPanel || !targetPanel.parentNode) {
                    return;
                }
                
                startX = e.pageX;
                const rect = targetPanel.getBoundingClientRect();
                startWidth = rect.width;
                parentWidth = targetPanel.parentNode.getBoundingClientRect().width;
                
                document.addEventListener('mousemove', onMouseMove, { passive: false });
                document.addEventListener('mouseup', onMouseUp, { once: true });
                resizer.classList.add('resizing');
                
                document.body.style.cursor = 'col-resize';
                document.body.style.userSelect = 'none';
                document.body.classList.add('dragging');
            } catch (error) {
                console.error("Error during mouse down:", error);
            }
        }
        
        function onMouseMove(e) {
            try {
                e.preventDefault();
                e.stopPropagation();
                
                const dx = e.pageX - startX;
                let newWidth = (startWidth + dx);
                
                if (!targetPanel.parentNode) {
                    onMouseUp();
                    return;
                }
                
                const currentParentWidth = parentWidth;
                const minWidth = currentParentWidth * (minPercent / 100);
                const maxWidth = currentParentWidth * (maxPercent / 100);
                
                if (newWidth < minWidth) newWidth = minWidth;
                if (newWidth > maxWidth) newWidth = maxWidth;
                
                const newWidthPercent = (newWidth / currentParentWidth) * 100;
                
                if (targetPanel && targetPanel.style) {
                    targetPanel.style.flex = `0 0 ${newWidthPercent}%`;
                    targetPanel.style.width = `${newWidthPercent}%`;
                }
            } catch (error) {
                console.error("Error during mouse move:", error);
                onMouseUp();
            }
        }
        
        function onMouseUp() {
            try {
                document.removeEventListener('mousemove', onMouseMove);
                document.removeEventListener('mouseup', onMouseUp);
                
                if (resizer) {
                    resizer.classList.remove('resizing');
                }
                
                document.body.style.cursor = '';
                document.body.style.userSelect = '';
                document.body.classList.remove('dragging');
            } catch (error) {
                console.error("Error during mouse up:", error);
            }
        }
        
        resizer.addEventListener('mousedown', onMouseDown);
        
        // Prevent TreeView drag events from interfering with resizer
        resizer.addEventListener('dragstart', (e) => {
            e.preventDefault();
            e.stopPropagation();
        });
        
        resizer.addEventListener('dragover', (e) => {
            e.preventDefault();
            e.stopPropagation();
        });
        
        resizer.addEventListener('drop', (e) => {
            e.preventDefault();
            e.stopPropagation();
        });
    }
    
    setupResizer(resizer1, panel1, 10, 50);
    
    if (resizer2 && panel2) {
        setupResizer(resizer2, panel2, 10, 90);
    }
};

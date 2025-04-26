window.initResizers = function () {
    console.log("Initializing resizers...");
    
    const resizer1 = document.getElementById('resizer1');
    const resizer2 = document.getElementById('resizer2');
    const panel1 = document.getElementById('panel1');
    const panel2 = document.getElementById('panel2');
    
    if (!resizer1 || !panel1) {
        console.error("Required elements not found for resizer1");
        return;
    }
    
    if (!resizer2 || !panel2) {
        console.error("Required elements not found for resizer2");
    }
    
    function setupResizer(resizer, targetPanel, minPercent, maxPercent) {
        let startX, startWidth, parentWidth;
        
        function onMouseDown(e) {
            e.preventDefault();
            startX = e.pageX;
            const rect = targetPanel.getBoundingClientRect();
            startWidth = rect.width;
            parentWidth = targetPanel.parentNode.getBoundingClientRect().width;
            
            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);
            resizer.classList.add('resizing');
            
            document.body.style.cursor = 'col-resize';
            document.body.style.userSelect = 'none';
        }
        
        function onMouseMove(e) {
            const dx = e.pageX - startX;
            let newWidth = (startWidth + dx);
            
            const minWidth = parentWidth * (minPercent / 100);
            const maxWidth = parentWidth * (maxPercent / 100);
            
            if (newWidth < minWidth) newWidth = minWidth;
            if (newWidth > maxWidth) newWidth = maxWidth;
            
            const newWidthPercent = (newWidth / parentWidth) * 100;
            
            targetPanel.style.flex = `0 0 ${newWidthPercent}%`;
            targetPanel.style.width = `${newWidthPercent}%`;
        }
        
        function onMouseUp() {
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
            resizer.classList.remove('resizing');
            
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
        }
        
        resizer.addEventListener('mousedown', onMouseDown);
    }
    
    setupResizer(resizer1, panel1, 10, 50);
    
    if (resizer2 && panel2) {
        setupResizer(resizer2, panel2, 10, 90);
    }
    
    console.log("Resizers initialized successfully");
};

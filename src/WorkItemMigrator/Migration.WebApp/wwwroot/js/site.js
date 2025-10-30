// Site-wide JavaScript functionality

// SignalR connection for real-time updates
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/migrationHub")
    .build();

// Start SignalR connection
connection.start().then(function () {
    console.log("SignalR connected");
}).catch(function (err) {
    console.error("SignalR connection failed: " + err.toString());
});

// Progress update handlers
connection.on("ExportProgress", function (progress) {
    updateProgressBar("export-progress", progress);
    updateProgressText("export-status", progress.message);
});

connection.on("ImportProgress", function (progress) {
    updateProgressBar("import-progress", progress);
    updateProgressText("import-status", progress.message);
});

function updateProgressBar(elementId, progress) {
    const progressBar = document.getElementById(elementId);
    if (progressBar) {
        progressBar.style.width = progress.percentage + "%";
        progressBar.setAttribute("aria-valuenow", progress.percentage);
        progressBar.textContent = progress.percentage + "%";
    }
}

function updateProgressText(elementId, message) {
    const statusElement = document.getElementById(elementId);
    if (statusElement) {
        statusElement.textContent = message;
    }
}

// Export management functions
function confirmExport() {
    if (confirm('Start the export with the current plan?')) {
        // Create a form to submit the confirmation
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = window.location.pathname;
        
        const handler = document.createElement('input');
        handler.type = 'hidden';
        handler.name = '__RequestVerificationToken';
        handler.value = document.querySelector('input[name="__RequestVerificationToken"]').value;
        form.appendChild(handler);
        
        const handlerInput = document.createElement('input');
        handlerInput.type = 'hidden';
        handlerInput.name = 'handler';
        handlerInput.value = 'ConfirmExport';
        form.appendChild(handlerInput);
        
        document.body.appendChild(form);
        form.submit();
    }
}

function cancelExport(exportId) {
    if (confirm('Cancel this export run?')) {
        fetch(`/Export/Cancel/${exportId}`, { 
            method: 'POST',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        })
        .then(response => {
            if (response.ok) {
                location.reload();
            } else {
                alert('Failed to cancel export');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Failed to cancel export');
        });
    }
}

// Form validation helpers
function validateForm(formId) {
    const form = document.getElementById(formId);
    if (form) {
        return form.checkValidity();
    }
    return false;
}

// Auto-refresh for running jobs
function startAutoRefresh(intervalSeconds = 5) {
    setInterval(() => {
        if (document.visibilityState === 'visible') {
            // Only refresh if page is visible
            const runningElements = document.querySelectorAll('.badge.bg-primary');
            if (runningElements.length > 0) {
                location.reload();
            }
        }
    }, intervalSeconds * 1000);
}
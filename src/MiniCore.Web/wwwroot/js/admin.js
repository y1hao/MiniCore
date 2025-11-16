document.addEventListener('DOMContentLoaded', function() {
    // Form submission handler
    const addLinkForm = document.getElementById('addLinkForm');
    if (addLinkForm) {
        addLinkForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            const submitBtn = document.getElementById('submitBtn');
            const messageContainer = document.getElementById('messageContainer');
            const originalUrl = document.getElementById('originalUrl').value;
            const shortCode = document.getElementById('shortCode').value.trim();
            const expiresAt = document.getElementById('expiresAt').value;
            
            // Clear previous messages
            messageContainer.innerHTML = '';
            
            // Disable submit button
            submitBtn.disabled = true;
            submitBtn.textContent = 'Creating...';
            
            try {
                const requestBody = {
                    originalUrl: originalUrl
                };
                
                if (shortCode) {
                    requestBody.shortCode = shortCode;
                }
                
                if (expiresAt) {
                    // Convert local datetime to ISO string
                    const date = new Date(expiresAt);
                    requestBody.expiresAt = date.toISOString();
                }
                
                const response = await fetch('/api/links', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(requestBody)
                });
                
                let data = null;
                const contentType = response.headers.get('content-type');
                
                // Only try to parse JSON if the response has content and is JSON
                if (contentType && contentType.includes('application/json')) {
                    const text = await response.text();
                    if (text) {
                        try {
                            data = JSON.parse(text);
                        } catch (e) {
                            console.error('Failed to parse JSON:', e);
                        }
                    }
                }
                
                if (response.ok) {
                    // Show success message
                    messageContainer.innerHTML = '<div class="message success">Short link created successfully! Refreshing...</div>';
                    
                    // Reset form
                    document.getElementById('addLinkForm').reset();
                    
                    // Reload page after a short delay
                    setTimeout(() => {
                        location.reload();
                    }, 1000);
                } else {
                    // Show error message
                    let errorMsg = 'Failed to create short link';
                    if (data) {
                        if (data.error) {
                            errorMsg = data.error;
                        } else if (data.title) {
                            errorMsg = data.title;
                        } else if (typeof data === 'string') {
                            errorMsg = data;
                        } else if (data.errors) {
                            // Handle ModelState errors
                            const errors = Object.values(data.errors).flat();
                            errorMsg = errors.join(', ');
                        }
                    } else {
                        errorMsg = `Error ${response.status}: ${response.statusText}`;
                    }
                    messageContainer.innerHTML = `<div class="message error">${errorMsg}</div>`;
                    submitBtn.disabled = false;
                    submitBtn.textContent = 'Create Short Link';
                }
            } catch (error) {
                messageContainer.innerHTML = `<div class="message error">Error: ${error.message}</div>`;
                submitBtn.disabled = false;
                submitBtn.textContent = 'Create Short Link';
            }
        });
    }

    // Delete button handlers using event delegation
    document.addEventListener('click', function(e) {
        if (e.target.classList.contains('delete-btn')) {
            const linkId = e.target.getAttribute('data-link-id');
            if (linkId) {
                deleteLink(parseInt(linkId));
            }
        }
    });
});

async function deleteLink(id) {
    if (!confirm('Are you sure you want to delete this link?')) {
        return;
    }

    try {
        const response = await fetch(`/api/links/${id}`, {
            method: 'DELETE'
        });

        if (response.ok) {
            location.reload();
        } else {
            alert('Failed to delete link');
        }
    } catch (error) {
        alert('Error deleting link: ' + error.message);
    }
}


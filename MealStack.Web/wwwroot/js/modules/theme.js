const ThemeModule = (function() {
    let currentTheme = 'light';

    const init = function() {
        console.log("🎨 Initializing MealStack Theme System...");

        loadSavedTheme();

        applyTheme(currentTheme);

        setupThemeToggle();

        // on profile page
        setupProfileThemeSelection();

        console.log("✅ Theme system initialized with theme:", currentTheme);
    };

    const loadSavedTheme = function() {
        const savedTheme = localStorage.getItem('mealstack-theme');

        const userTheme = document.documentElement.getAttribute('data-user-theme');

        // Priority: localStorage > user preference > light default
        currentTheme = savedTheme || userTheme || 'light';

        console.log("📱 Loaded theme:", currentTheme);
    };

    const applyTheme = function(theme) {
        const html = document.documentElement;

        html.removeAttribute('data-theme');

        if (theme === 'dark') {
            html.setAttribute('data-theme', 'dark');
        }

        updateThemeIndicator(theme);

        localStorage.setItem('mealstack-theme', theme);

        console.log("🎨 Applied theme:", theme);
    };

    const updateThemeIndicator = function(theme) {
        const themeIcon = document.getElementById('theme-icon');
        const themeIndicator = document.querySelector('.theme-indicator');

        if (themeIcon) {
            themeIcon.className = theme === 'dark' ? 'bi bi-moon-fill' : 'bi bi-sun-fill';
        }

        if (themeIndicator) {
            themeIndicator.innerHTML = theme === 'dark' ? '🌙' : '☀️';
            themeIndicator.title = `Current theme: ${theme}`;
        }
    };

    const setupThemeToggle = function() {
        const themeToggle = document.getElementById('theme-toggle');

        if (themeToggle) {
            themeToggle.addEventListener('click', function(e) {
                e.preventDefault();
                toggleTheme();
            });

            console.log("🔘 Theme toggle button found and configured");
        }
    };

    const setupProfileThemeSelection = function() {
        // Check if we're on the profile page
        const themeRadios = document.querySelectorAll('input[name="ThemePreference"]');
        const saveThemeBtn = document.getElementById('saveThemeBtn');

        if (themeRadios.length > 0) {
            console.log("👤 Profile theme selection found");

            themeRadios.forEach(radio => {
                if (radio.value === currentTheme) {
                    radio.checked = true;
                }

                // Change listener for live preview
                radio.addEventListener('change', function() {
                    if (this.checked) {
                        previewTheme(this.value);
                    }
                });
            });

            if (saveThemeBtn) {
                saveThemeBtn.addEventListener('click', function() {
                    const selectedTheme = document.querySelector('input[name="ThemePreference"]:checked')?.value;
                    if (selectedTheme) {
                        saveThemePreference(selectedTheme);
                    }
                });
            }
        }
    };

    const toggleTheme = function() {
        const newTheme = currentTheme === 'light' ? 'dark' : 'light';
        currentTheme = newTheme;
        applyTheme(newTheme);

        console.log("🔄 Theme toggled to:", newTheme);

        if (typeof saveThemePreference === 'function') {
            saveThemePreference(newTheme);
        }
    };

    const previewTheme = function(theme) {
        applyTheme(theme);
        currentTheme = theme;

        console.log("👁️ Previewing theme:", theme);
    };

    const saveThemePreference = function(theme) {
        // Save to server via AJAX
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        if (!token) {
            console.warn("⚠️ No anti-forgery token found");
            return;
        }

        const formData = new FormData();
        formData.append('ThemePreference', theme);
        formData.append('__RequestVerificationToken', token);

        fetch('/Account/UpdateThemePreference', {
            method: 'POST',
            body: formData
        })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    console.log("✅ Theme preference saved to server:", theme);

                    showNotification('Theme updated successfully! 🎨', 'success');

                    currentTheme = theme;
                    applyTheme(theme);
                } else {
                    console.error("❌ Failed to save theme:", data.message);
                    showNotification('Failed to save theme preference', 'error');
                }
            })
            .catch(error => {
                console.error("❌ Error saving theme:", error);
                showNotification('Error saving theme preference', 'error');
            });
    };

    const showNotification = function(message, type = 'info') {
        // Remove existing notifications
        const existing = document.querySelector('.theme-notification');
        if (existing) {
            existing.remove();
        }
        
        const notification = document.createElement('div');
        notification.className = `alert alert-${type === 'success' ? 'success' : 'danger'} alert-dismissible fade show theme-notification`;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
            min-width: 300px;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.15);
        `;

        notification.innerHTML = `
            <i class="bi bi-${type === 'success' ? 'check-circle' : 'exclamation-triangle'} me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;

        document.body.appendChild(notification);

        setTimeout(() => {
            if (notification && notification.parentNode) {
                notification.remove();
            }
        }, 3000);
    };

    const getCurrentTheme = function() {
        return currentTheme;
    };

    const setTheme = function(theme) {
        if (theme === 'light' || theme === 'dark') {
            currentTheme = theme;
            applyTheme(theme);
            return true;
        }
        return false;
    };

    // Public API
    return {
        init: init,
        toggleTheme: toggleTheme,
        getCurrentTheme: getCurrentTheme,
        setTheme: setTheme,
        previewTheme: previewTheme
    };
})();

function toggleTheme() {
    if (typeof ThemeModule !== 'undefined') {
        ThemeModule.toggleTheme();
    }
}

document.addEventListener('DOMContentLoaded', function() {
    if (typeof ThemeModule !== 'undefined') {
        ThemeModule.init();
    }
});

window.ThemeModule = ThemeModule;
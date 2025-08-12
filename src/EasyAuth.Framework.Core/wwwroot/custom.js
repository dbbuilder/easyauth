/* EasyAuth Framework Swagger UI Custom JavaScript */

(function() {
    'use strict';
    
    // Wait for Swagger UI to load
    document.addEventListener('DOMContentLoaded', function() {
        // Add custom functionality after a brief delay to ensure UI is ready
        setTimeout(function() {
            initializeEasyAuthEnhancements();
        }, 1000);
    });

    function initializeEasyAuthEnhancements() {
        // Add helpful tooltips
        addHelpfulTooltips();
        
        // Enhance authentication section
        enhanceAuthenticationSection();
        
        // Add example values automatically
        addExampleValues();
        
        // Add copy-to-clipboard functionality
        addCopyToClipboard();
        
        // Add developer tips
        addDeveloperTips();
        
        console.log('🚀 EasyAuth Framework Swagger UI enhancements loaded!');
    }

    function addHelpfulTooltips() {
        // Add tooltips to common elements
        const tryItButtons = document.querySelectorAll('.try-out__btn');
        tryItButtons.forEach(button => {
            button.title = 'Click to enable interactive testing of this endpoint';
        });

        const executeButtons = document.querySelectorAll('.btn.execute');
        executeButtons.forEach(button => {
            button.title = 'Send the request and see the actual response';
        });

        // Add provider-specific help
        const providerInputs = document.querySelectorAll('input[placeholder*="provider"], input[data-param-name="provider"]');
        providerInputs.forEach(input => {
            const helpDiv = document.createElement('div');
            helpDiv.className = 'parameter-help';
            helpDiv.innerHTML = `
                <small style="color: #666; font-style: italic;">
                    💡 Try: <code>google</code>, <code>facebook</code>, <code>apple</code>, or <code>azureb2c</code>
                </small>
            `;
            input.parentNode.insertBefore(helpDiv, input.nextSibling);
        });
    }

    function enhanceAuthenticationSection() {
        // Add visual indicators for authentication requirements
        const authSections = document.querySelectorAll('.auth-container');
        authSections.forEach(section => {
            const tip = document.createElement('div');
            tip.className = 'auth-tip';
            tip.innerHTML = `
                <div style="background: #e8f5e8; padding: 12px; border-radius: 6px; margin: 10px 0; border-left: 4px solid #4caf50;">
                    <strong>🔐 Authentication Tip:</strong><br>
                    Get your Bearer token by calling <code>/api/auth/login/{provider}</code> first!
                </div>
            `;
            section.appendChild(tip);
        });
    }

    function addExampleValues() {
        // Auto-fill common example values
        const inputs = document.querySelectorAll('input[type="text"]');
        inputs.forEach(input => {
            const placeholder = input.placeholder?.toLowerCase() || '';
            const name = input.getAttribute('data-param-name')?.toLowerCase() || '';
            
            if (placeholder.includes('redirect') || name.includes('redirect')) {
                input.placeholder = 'https://myapp.com/auth/callback';
            } else if (placeholder.includes('state') || name.includes('state')) {
                input.placeholder = 'random_state_' + Math.random().toString(36).substr(2, 9);
            } else if (placeholder.includes('email') || name.includes('email')) {
                input.placeholder = 'user@example.com';
            }
        });
    }

    function addCopyToClipboard() {
        // Add copy buttons to code blocks
        const codeBlocks = document.querySelectorAll('.highlight-code pre');
        codeBlocks.forEach(block => {
            const copyButton = document.createElement('button');
            copyButton.className = 'copy-button';
            copyButton.innerHTML = '📋 Copy';
            copyButton.style.cssText = `
                position: absolute;
                top: 8px;
                right: 8px;
                background: #2c5aa0;
                color: white;
                border: none;
                border-radius: 4px;
                padding: 4px 8px;
                font-size: 12px;
                cursor: pointer;
                opacity: 0;
                transition: opacity 0.2s;
            `;
            
            block.parentNode.style.position = 'relative';
            block.parentNode.appendChild(copyButton);
            
            // Show copy button on hover
            block.parentNode.addEventListener('mouseenter', () => {
                copyButton.style.opacity = '1';
            });
            
            block.parentNode.addEventListener('mouseleave', () => {
                copyButton.style.opacity = '0';
            });
            
            // Copy functionality
            copyButton.addEventListener('click', async () => {
                try {
                    await navigator.clipboard.writeText(block.textContent);
                    copyButton.innerHTML = '✅ Copied!';
                    copyButton.style.background = '#4caf50';
                    setTimeout(() => {
                        copyButton.innerHTML = '📋 Copy';
                        copyButton.style.background = '#2c5aa0';
                    }, 2000);
                } catch (err) {
                    console.error('Failed to copy text: ', err);
                    copyButton.innerHTML = '❌ Failed';
                    setTimeout(() => {
                        copyButton.innerHTML = '📋 Copy';
                    }, 2000);
                }
            });
        });
    }

    function addDeveloperTips() {
        // Add a helpful developer tips section
        const info = document.querySelector('.swagger-ui .info');
        if (info) {
            const tipsSection = document.createElement('div');
            tipsSection.className = 'developer-tips';
            tipsSection.innerHTML = `
                <div style="background: linear-gradient(135deg, #f0f9ff 0%, #e0f2fe 100%); border: 1px solid #0284c7; border-radius: 8px; padding: 20px; margin: 20px 0;">
                    <h3 style="color: #0369a1; margin-top: 0;">🎯 Quick Start Guide</h3>
                    <ol style="margin: 0; padding-left: 20px; line-height: 1.6;">
                        <li><strong>Configure providers</strong> in your <code>appsettings.json</code></li>
                        <li><strong>Call login endpoint:</strong> <code>POST /api/auth/login/{provider}</code></li>
                        <li><strong>Copy the access token</strong> from the response</li>
                        <li><strong>Click "Authorize" above</strong> and paste: <code>Bearer YOUR_TOKEN</code></li>
                        <li><strong>Test protected endpoints</strong> with your authenticated session</li>
                    </ol>
                    <div style="margin-top: 15px; padding: 10px; background: rgba(6, 182, 212, 0.1); border-radius: 6px;">
                        <strong>💡 Pro Tip:</strong> Use the "Try it out" feature to test authentication flows directly!
                    </div>
                </div>
            `;
            info.appendChild(tipsSection);
        }

        // Add endpoint-specific tips
        const operations = document.querySelectorAll('.opblock');
        operations.forEach(operation => {
            const summary = operation.querySelector('.opblock-summary-description');
            if (summary && summary.textContent.toLowerCase().includes('login')) {
                const tip = document.createElement('div');
                tip.style.cssText = `
                    background: #fff3cd;
                    border: 1px solid #ffeaa7;
                    border-radius: 6px;
                    padding: 12px;
                    margin: 10px 0;
                    color: #856404;
                `;
                tip.innerHTML = `
                    <strong>🚀 Start Here:</strong> This is typically the first endpoint you'll use. 
                    It initiates the OAuth flow and returns tokens for authentication.
                `;
                
                const responseSection = operation.querySelector('.responses-wrapper');
                if (responseSection) {
                    responseSection.insertBefore(tip, responseSection.firstChild);
                }
            }
        });
    }

    // Add keyboard shortcuts for better UX
    document.addEventListener('keydown', function(e) {
        // Ctrl+Shift+A to focus on Authorization
        if (e.ctrlKey && e.shiftKey && e.key === 'A') {
            const authorizeBtn = document.querySelector('.btn.authorize');
            if (authorizeBtn) {
                authorizeBtn.click();
                e.preventDefault();
            }
        }
        
        // Ctrl+Shift+T to try out first endpoint
        if (e.ctrlKey && e.shiftKey && e.key === 'T') {
            const firstTryOut = document.querySelector('.try-out__btn');
            if (firstTryOut) {
                firstTryOut.click();
                e.preventDefault();
            }
        }
    });

    // Add a keyboard shortcuts help
    const shortcutsHelp = document.createElement('div');
    shortcutsHelp.style.cssText = `
        position: fixed;
        bottom: 20px;
        right: 20px;
        background: rgba(44, 90, 160, 0.9);
        color: white;
        padding: 10px;
        border-radius: 6px;
        font-size: 12px;
        opacity: 0.7;
        z-index: 1000;
        transition: opacity 0.2s;
    `;
    shortcutsHelp.innerHTML = `
        <strong>⌨️ Shortcuts:</strong><br>
        Ctrl+Shift+A: Authorize<br>
        Ctrl+Shift+T: Try first endpoint
    `;
    
    shortcutsHelp.addEventListener('mouseenter', () => {
        shortcutsHelp.style.opacity = '1';
    });
    
    shortcutsHelp.addEventListener('mouseleave', () => {
        shortcutsHelp.style.opacity = '0.7';
    });

    setTimeout(() => {
        document.body.appendChild(shortcutsHelp);
    }, 2000);

})();
// Fonctions principales du site
$(document).ready(function() {
    // Gestion du sidebar toggle
    $('#sidebar-toggle').click(function() {
        $('#sidebar').toggleClass('collapsed');
        $('.main-content').toggleClass('expanded');
    });

    // Initialisation des tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Gestion de l'authentification
    setupAuth();
});

// Initialisation de l'authentification
function setupAuth() {
    // Vérifier si l'utilisateur est connecté
    const token = localStorage.getItem('jwt_token');
    const username = localStorage.getItem('username');

    if (token && username) {
        // Utilisateur connecté
        $('#username').text(username);

        // Ajouter le token aux en-têtes pour les requêtes AJAX
        $.ajaxSetup({
            headers: {
                'Authorization': 'Bearer ' + token
            }
        });
    } else {
        // Utilisateur non connecté, afficher la modal de connexion
        setTimeout(() => {
            $('#loginModal').modal('show');
        }, 500);
    }

    // Gestion du formulaire de connexion
    $('#loginForm').submit(function(e) {
        e.preventDefault();

        const username = $('#username').val();
        const password = $('#password').val();

        $.ajax({
            url: '/api/auth/login',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ username, password }),
            success: function(response) {
                // Enregistrer le token et le nom d'utilisateur
                localStorage.setItem('jwt_token', response.token);
                localStorage.setItem('username', response.username);
                localStorage.setItem('role', response.role);

                // Mettre à jour l'UI
                $('#username').text(response.username);

                // Configurer AJAX pour inclure le token dans toutes les requêtes
                $.ajaxSetup({
                    headers: {
                        'Authorization': 'Bearer ' + response.token
                    }
                });

                // Fermer la modal
                $('#loginModal').modal('hide');

                // Recharger la page
                setTimeout(() => {
                    location.reload();
                }, 500);
            },
            error: function(xhr) {
                // Afficher l'erreur
                $('#loginError').removeClass('d-none').text(xhr.responseJSON?.message || 'Erreur de connexion');
            }
        });
    });

    // Gestion de la déconnexion
    $('#logout-link').click(function(e) {
        e.preventDefault();

        // Supprimer les données d'authentification
        localStorage.removeItem('jwt_token');
        localStorage.removeItem('username');
        localStorage.removeItem('role');

        // Rediriger vers la page d'accueil
        window.location.href = '/';
    });
}

// Fonctions utilitaires
function formatNumber(num, decimals = 2) {
    return num.toFixed(decimals).replace(/\B(?=(\d{3})+(?!\d))/g, ',');
}

function formatCurrency(value, currency = 'USDT', decimals = 2) {
    return `${formatNumber(value, decimals)} ${currency}`;
}

function formatDate(date) {
    return new Date(date).toLocaleString();
}

// Fonctions pour gérer les notifications
function showNotification(message, type = 'info') {
    const iconClass = {
        'success': 'bi-check-circle',
        'danger': 'bi-exclamation-triangle',
        'warning': 'bi-exclamation-circle',
        'info': 'bi-info-circle'
    };

    const notificationHtml = `
        <div class="toast align-items-center text-white bg-${type} border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="bi ${iconClass[type]}"></i> ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;

    const toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        const container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        document.body.appendChild(container);
    }

    const toastElement = $(notificationHtml);
    $('#toast-container').append(toastElement);
    const toast = new bootstrap.Toast(toastElement, { delay: 5000 });
    toast.show();
}

// Fonctions pour les pages spécifiques
// Dashboard
function refreshDashboardData() {
    // Cette fonction peut être utilisée pour mettre à jour les données du dashboard via AJAX
    $.ajax({
        url: '/api/dashboard/summary',
        type: 'GET',
        success: function(data) {
            // Mise à jour des statistiques
            updateDashboardStats(data);
            // Mise à jour du graphique de performance
            updatePerformanceChart(data);
            // Mise à jour des positions récentes
            updateRecentPositions(data.recentPositions);
            // Mise à jour des prix actuels
            updateCurrentPrices(data.currentPrices);
        },
        error: function(xhr) {
            showNotification('Erreur lors du rafraîchissement des données', 'danger');
        }
    });
}

function updateDashboardStats(data) {
    // Mise à jour des statistiques globales
    $('#total-profit').text(formatCurrency(data.totalProfit));
    $('#daily-profit').text(formatCurrency(data.dailyProfit));
    $('#open-positions').text(data.openPositions);
    $('#win-rate').text(`${(data.winRate * 100).toFixed(2)}%`);

    // Mise à jour des statistiques détaillées
    $('#winning-trades').text(data.winningTrades);
    $('#losing-trades').text(data.losingTrades);
    $('#average-profit').text(formatCurrency(data.averageProfit));
    $('#average-loss').text(formatCurrency(data.averageLoss));
    $('#profit-factor').text(data.profitFactor.toFixed(2));
    $('#max-drawdown').text(formatCurrency(data.maxDrawdown));
}

function updatePerformanceChart(data) {
    // Mise à jour du graphique avec les nouvelles données
    if (window.performanceChart) {
        window.performanceChart.data.labels = data.equityCurve.map(point => new Date(point.date).toLocaleDateString());
        window.performanceChart.data.datasets[0].data = data.equityCurve.map(point => point.equity);
        window.performanceChart.update();
    }
}

function updateRecentPositions(positions) {
    // Mise à jour du tableau des positions récentes
    const positionsTable = $('#recent-positions-table tbody');
    positionsTable.empty();

    positions.forEach(position => {
        const row = `
            <tr class="position-row ${position.status === 'Open' ? 'active-position' : ''}">
                <td>${position.symbol}</td>
                <td>
                    <span class="badge bg-${position.type === 'Long' ? 'success' : 'danger'}">${position.type}</span>
                </td>
                <td>${formatNumber(position.entryPrice)}</td>
                <td>${position.exitPrice ? formatNumber(position.exitPrice) : '-'}</td>
                <td>${formatNumber(position.quantity, 6)}</td>
                <td class="profit-${position.currentProfit >= 0 ? 'positive' : 'negative'}">
                    ${formatNumber(position.currentProfit)} (${formatNumber(position.currentProfitPercentage)}%)
                </td>
                <td>
                    <span class="badge bg-${position.status === 'Open' ? 'primary' : 'secondary'}">${position.status}</span>
                </td>
                <td>
                    <div class="btn-group">
                        <button type="button" class="btn btn-sm btn-outline-primary view-position" data-id="${position.id}">
                            <i class="bi bi-eye"></i>
                        </button>
                        ${position.status === 'Open' ? `
                        <button type="button" class="btn btn-sm btn-outline-danger close-position" data-id="${position.id}">
                            <i class="bi bi-x-circle"></i>
                        </button>
                        ` : ''}
                    </div>
                </td>
            </tr>
        `;
        positionsTable.append(row);
    });

    // Réattacher les gestionnaires d'événements
    attachPositionEventHandlers();
}

function updateCurrentPrices(prices) {
    // Mise à jour du tableau des prix actuels
    Object.entries(prices).forEach(([symbol, price]) => {
        const row = $(`.price-row[data-symbol="${symbol}"]`);
        const priceElement = row.find('.current-price');
        const oldPrice = parseFloat(priceElement.text());
        const newPrice = price;

        // Animation si le prix a changé
        if (oldPrice !== newPrice) {
            priceElement.text(formatNumber(newPrice));
            row.addClass('highlight');
            setTimeout(() => {
                row.removeClass('highlight');
            }, 1000);
        }
    });
}

function attachPositionEventHandlers() {
    // Gestionnaire pour afficher les détails d'une position
    $('.view-position').click(function() {
        const positionId = $(this).data('id');
        window.location.href = `/Positions/Details/${positionId}`;
    });

    // Gestionnaire pour fermer une position
    $('.close-position').click(function() {
        const positionId = $(this).data('id');
        if (confirm('Êtes-vous sûr de vouloir fermer cette position ?')) {
            $.ajax({
                url: `/api/positions/${positionId}/close`,
                type: 'POST',
                success: function(result) {
                    showNotification('Position fermée avec succès', 'success');
                    // Rafraîchir les données
                    refreshDashboardData();
                },
                error: function(xhr) {
                    showNotification(`Erreur: ${xhr.responseJSON?.message || 'Impossible de fermer la position'}`, 'danger');
                }
            });
        }
    });
}

// Trading Pairs
function initTradingPairsPage() {
    // Chargement initial des paires de trading
    loadTradingPairs();

    // Gestionnaire pour le formulaire d'ajout de paire
    $('#add-pair-form').submit(function(e) {
        e.preventDefault();

        const formData = {
            symbol: $('#pair-symbol').val(),
            baseAsset: $('#pair-base-asset').val(),
            quoteAsset: $('#pair-quote-asset').val(),
            pricePrecision: parseInt($('#pair-price-precision').val(), 10),
            quantityPrecision: parseInt($('#pair-quantity-precision').val(), 10),
            minNotional: parseFloat($('#pair-min-notional').val()),
            minQuantity: parseFloat($('#pair-min-quantity').val()),
            maxQuantity: parseFloat($('#pair-max-quantity').val()),
            stepSize: parseFloat($('#pair-step-size').val()),
            tickSize: parseFloat($('#pair-tick-size').val()),
            isActive: $('#pair-is-active').prop('checked')
        };

        $.ajax({
            url: '/api/tradingpairs',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(formData),
            success: function(result) {
                showNotification('Paire de trading ajoutée avec succès', 'success');
                $('#add-pair-modal').modal('hide');
                loadTradingPairs();
            },
            error: function(xhr) {
                showNotification(`Erreur: ${xhr.responseJSON?.message || 'Impossible d\'ajouter la paire'}`, 'danger');
            }
        });
    });
}

function loadTradingPairs() {
    $.ajax({
        url: '/api/tradingpairs',
        type: 'GET',
        success: function(pairs) {
            const pairsTable = $('#trading-pairs-table tbody');
            pairsTable.empty();

            pairs.forEach(pair => {
                const row = `
                    <tr data-symbol="${pair.symbol}">
                        <td>${pair.symbol}</td>
                        <td>${pair.baseAsset}</td>
                        <td>${pair.quoteAsset}</td>
                        <td>${pair.pricePrecision}</td>
                        <td>${pair.quantityPrecision}</td>
                        <td>${formatNumber(pair.minNotional)}</td>
                        <td>${formatNumber(pair.minQuantity, pair.quantityPrecision)}</td>
                        <td>
                            <div class="form-check form-switch">
                                <input class="form-check-input toggle-pair-active" type="checkbox"
                                   data-symbol="${pair.symbol}" ${pair.isActive ? 'checked' : ''}>
                            </div>
                        </td>
                        <td>
                            <div class="btn-group">
                                <button type="button" class="btn btn-sm btn-outline-primary edit-pair" data-symbol="${pair.symbol}">
                                    <i class="bi bi-pencil"></i>
                                </button>
                            </div>
                        </td>
                    </tr>
                `;
                pairsTable.append(row);
            });

            // Attacher les gestionnaires d'événements
            attachTradingPairEventHandlers();
        },
        error: function(xhr) {
            showNotification('Erreur lors du chargement des paires de trading', 'danger');
        }
    });
}

function attachTradingPairEventHandlers() {
    // Gestionnaire pour basculer l'état actif d'une paire
    $('.toggle-pair-active').change(function() {
        const symbol = $(this).data('symbol');
        const isActive = $(this).prop('checked');

        $.ajax({
            url: `/api/tradingpairs/${symbol}/toggle-active`,
            type: 'PATCH',
            success: function() {
                showNotification(`Paire ${symbol} ${isActive ? 'activée' : 'désactivée'}`, 'success');
            },
            error: function(xhr) {
                showNotification(`Erreur: ${xhr.responseJSON?.message || 'Impossible de modifier la paire'}`, 'danger');
                // Rétablir l'état précédent
                $(this).prop('checked', !isActive);
            }
        });
    });

    // Gestionnaire pour éditer une paire
    $('.edit-pair').click(function() {
        const symbol = $(this).data('symbol');

        // Charger les détails de la paire
        $.ajax({
            url: `/api/tradingpairs/${symbol}`,
            type: 'GET',
            success: function(pair) {
                // Remplir le formulaire d'édition
                $('#edit-pair-symbol').val(pair.symbol);
                $('#edit-pair-base-asset').val(pair.baseAsset);
                $('#edit-pair-quote-asset').val(pair.quoteAsset);
                $('#edit-pair-price-precision').val(pair.pricePrecision);
                $('#edit-pair-quantity-precision').val(pair.quantityPrecision);
                $('#edit-pair-min-notional').val(pair.minNotional);
                $('#edit-pair-min-quantity').val(pair.minQuantity);
                $('#edit-pair-max-quantity').val(pair.maxQuantity);
                $('#edit-pair-step-size').val(pair.stepSize);
                $('#edit-pair-tick-size').val(pair.tickSize);
                $('#edit-pair-is-active').prop('checked', pair.isActive);

                // Afficher la modal d'édition
                $('#edit-pair-modal').modal('show');
            },
            error: function(xhr) {
                showNotification(`Erreur: ${xhr.responseJSON?.message || 'Impossible de charger les détails de la paire'}`, 'danger');
            }
        });
    });

    // Gestionnaire pour le formulaire d'édition
    $('#edit-pair-form').submit(function(e) {
        e.preventDefault();

        const symbol = $('#edit-pair-symbol').val();
        const formData = {
            symbol: symbol,
            baseAsset: $('#edit-pair-base-asset').val(),
            quoteAsset: $('#edit-pair-quote-asset').val(),
            pricePrecision: parseInt($('#edit-pair-price-precision').val(), 10),
            quantityPrecision: parseInt($('#edit-pair-quantity-precision').val(), 10),
            minNotional: parseFloat($('#edit-pair-min-notional').val()),
            minQuantity: parseFloat($('#edit-pair-min-quantity').val()),
            maxQuantity: parseFloat($('#edit-pair-max-quantity').val()),
            stepSize: parseFloat($('#edit-pair-step-size').val()),
            tickSize: parseFloat($('#edit-pair-tick-size').val()),
            isActive: $('#edit-pair-is-active').prop('checked')
        };

        $.ajax({
            url: `/api/tradingpairs/${symbol}`,
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(formData),
            success: function() {
                showNotification('Paire de trading mise à jour avec succès', 'success');
                $('#edit-pair-modal').modal('hide');
                loadTradingPairs();
            },
            error: function(xhr) {
                showNotification(`Erreur: ${xhr.responseJSON?.message || 'Impossible de mettre à jour la paire'}`, 'danger');
            }
        });
    });
}

// Settings
function initSettingsPage() {
    // Chargement initial des paramètres
    loadSettings();

    // Gestionnaire pour le formulaire des paramètres généraux
    $('#general-settings-form').submit(function(e) {
        e.preventDefault();

        const formData = {
            useTestnet: $('#use-testnet').prop('checked'),
            defaultStrategy: $('#default-strategy').val(),
            refreshInterval: parseInt($('#refresh-interval').val(), 10),
            minConfidenceThreshold: parseFloat($('#min-confidence-threshold').val()),
            riskPerTradePercentage: parseFloat($('#risk-per-trade').val()),
            minOrderAmount: parseFloat($('#min-order-amount').val()),
            allowMultiplePositions: $('#allow-multiple-positions').prop('checked'),
            useStopLoss: $('#use-stop-loss').prop('checked'),
            useTakeProfit: $('#use-take-profit').prop('checked'),
            stopLossPercentage: parseFloat($('#stop-loss-percentage').val()),
            takeProfitPercentage: parseFloat($('#take-profit-percentage').val()),
            useDynamicStopLoss: $('#use-dynamic-stop-loss').prop('checked')
        };

        $.ajax({
            url: '/api/settings',
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(formData),
            success: function() {
                showNotification('Paramètres mis à jour avec succès', 'success');
            },
            error: function(xhr) {
                showNotification(`Erreur: ${xhr.responseJSON?.message || 'Impossible de mettre à jour les paramètres'}`, 'danger');
            }
        });
    });

    // Gestionnaire pour le formulaire des paramètres de risque
    $('#risk-management-form').submit(function(e) {
        e.preventDefault();

        const formData = {
            maxPortfolioExposure: parseFloat($('#max-portfolio-exposure').val()),
            criticalExposureThreshold: parseFloat($('#critical-exposure-threshold').val()),
            maxPositionSize: parseFloat($('#max-position-size').val()),
            maxAllowedVolatility: parseFloat($('#max-allowed-volatility').val()),
            emergencyExitThreshold: parseFloat($('#emergency-exit-threshold').val()),
            maxPositionDays: parseInt($('#max-position-days').val(), 10),
            maxDailyLoss: parseFloat($('#max-daily-loss').val())
        };

        $.ajax({
            url: '/api/settings/risk-management',
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(formData),
            success: function() {
                showNotification('Paramètres de gestion du risque mis à jour avec succès', 'success');
            },
            error: function(xhr) {
                showNotification(`Erreur: ${xhr.responseJSON?.message || 'Impossible de mettre à jour les paramètres'}`, 'danger');
            }
        });
    });

    // Gestionnaire pour le formulaire des informations d'API
    $('#api-credentials-form').submit(function(e) {
        e.preventDefault();

        const formData = {
            apiKey: $('#api-key').val(),
            apiSecret: $('#api-secret').val(),
            useTestnet: $('#api-use-testnet').prop('checked')
        };

        $.ajax({
            url: '/api/settings/api-credentials',
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(formData),
            success: function() {
                showNotification('Informations d\'API mises à jour avec succès', 'success');
            },
            error: function(xhr) {
                showNotification(`Erreur: ${xhr.responseJSON?.message || 'Impossible de mettre à jour les informations d\'API'}`, 'danger');
            }
        });
    });

    // Gestionnaire pour le formulaire des paramètres de notification
    $('#notification-settings-form').submit(function(e) {
        e.preventDefault();

        const formData = {
            enableEmailNotifications: $('#enable-email-notifications').prop('checked'),
            emailApiKey: $('#email-api-key').val(),
            emailSender: $('#email-sender').val(),
            emailRecipient: $('#email-recipient').val(),
            enableTelegramNotifications: $('#enable-telegram-notifications').prop('checked'),
            telegramBotToken: $('#telegram-bot-token').val(),
            telegramChatId: $('#telegram-chat-id').val()
        };

        $.ajax({
            url: '/api/settings/notifications',
            type: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(formData),
            success: function() {
                showNotification('Paramètres de notification mis à jour avec succès', 'success');
            },
            error: function(xhr) {
                showNotification(`Erreur: ${xhr.responseJSON?.message || 'Impossible de mettre à jour les paramètres'}`, 'danger');
            }
        });
    });
}

function loadSettings() {
    $.ajax({
        url: '/api/settings',
        type: 'GET',
        success: function(settings) {
            // Remplir le formulaire des paramètres généraux
            $('#use-testnet').prop('checked', settings.useTestnet);
            $('#default-strategy').val(settings.defaultStrategy);
            $('#refresh-interval').val(settings.refreshInterval);
            $('#min-confidence-threshold').val(settings.minConfidenceThreshold);
            $('#risk-per-trade').val(settings.riskPerTradePercentage);
            $('#min-order-amount').val(settings.minOrderAmount);
            $('#allow-multiple-positions').prop('checked', settings.allowMultiplePositions);
            $('#use-stop-loss').prop('checked', settings.useStopLoss);
            $('#use-take-profit').prop('checked', settings.useTakeProfit);
            $('#stop-loss-percentage').val(settings.stopLossPercentage);
            $('#take-profit-percentage').val(settings.takeProfitPercentage);
            $('#use-dynamic-stop-loss').prop('checked', settings.useDynamicStopLoss);

            // Charger les paramètres de risque
            $.ajax({
                url: '/api/settings/risk-management',
                type: 'GET',
                success: function(riskSettings) {
                    $('#max-portfolio-exposure').val(riskSettings.maxPortfolioExposure);
                    $('#critical-exposure-threshold').val(riskSettings.criticalExposureThreshold);
                    $('#max-position-size').val(riskSettings.maxPositionSize);
                    $('#max-allowed-volatility').val(riskSettings.maxAllowedVolatility);
                    $('#emergency-exit-threshold').val(riskSettings.emergencyExitThreshold);
                    $('#max-position-days').val(riskSettings.maxPositionDays);
                    $('#max-daily-loss').val(riskSettings.maxDailyLoss);
                }
            });

            // Charger les paramètres de notification
            $('#enable-email-notifications').prop('checked', settings.enableEmailNotifications);
            $('#enable-telegram-notifications').prop('checked', settings.enableTelegramNotifications);
        },
        error: function(xhr) {
            showNotification('Erreur lors du chargement des paramètres', 'danger');
        }
    });
}
// ==================== KitaKo JavaScript - Complete Application Logic ====================
// Client-side functionality backed by server APIs

// ==================== DATA STORAGE ====================

let sales = [];
let utangs = [];
let expenses = [];
let availableBudget = 0;
let dailyGoal = 1000;

async function fetchJson(url, options = {}) {
    const response = await fetch(url, {
        credentials: 'same-origin',
        ...options,
        headers: {
            ...(options.body ? { 'Content-Type': 'application/json' } : {}),
            ...(options.headers || {})
        }
    });

    if (response.status === 401) {
        throw new Error('Unauthorized');
    }

    if (response.status === 204) {
        return null;
    }

    const data = await response.json();

    if (!response.ok) {
        throw new Error(data.message || `Request failed: ${response.status}`);
    }

    return data;
}

function notifyDataChanged() {
    window.dispatchEvent(new Event('kitako-data-changed'));
}

function refreshDataFromStorage() {
    // Server APIs are now the source of truth. This remains for older event hooks.
}

async function refreshSalesFromServer() {
    sales = await fetchJson('/api/sales');
}

async function refreshExpensesFromServer() {
    expenses = await fetchJson('/api/expenses');
}

async function refreshFinancialSettingsFromServer() {
    const settings = await fetchJson('/api/settings/financial');
    availableBudget = parseFloat(settings.availableBudget) || 0;
    dailyGoal = parseFloat(settings.dailySalesGoal) || 1000;
}

async function saveFinancialSettings() {
    const settings = await fetchJson('/api/settings/financial', {
        method: 'PUT',
        body: JSON.stringify({
            availableBudget,
            dailySalesGoal: dailyGoal
        })
    });

    availableBudget = parseFloat(settings.availableBudget) || 0;
    dailyGoal = parseFloat(settings.dailySalesGoal) || 1000;
    notifyDataChanged();
}

// Refresh utangs from server
async function refreshUtangsFromServer() {
    try {
        const response = await fetch('/api/utangs', { credentials: 'same-origin' });
        if (response.status === 401) {
            console.warn('Not authenticated when fetching utangs (server returned 401).');
            return;
        }
        if (response.ok) {
            const serverUtangs = await response.json();
            utangs = serverUtangs;
        }
    } catch (error) {
        console.error('Error fetching utangs from server:', error);
    }
}

// ==================== DAILY GOAL PERSISTENCE ====================

function getDailyGoal() {
    const n = parseFloat(dailyGoal);
    if (!isFinite(n) || n <= 0) return 1000;
    return n;
}

async function setDailyGoal(value) {
    dailyGoal = value;
    await saveFinancialSettings();
}

// ==================== MODAL FUNCTIONS ====================

function openSaleModal() {
    const elModal = document.getElementById('saleModal');
    if (!elModal) return;
    elModal.classList.remove('hidden');
    const el = document.getElementById('saleAmount');
    if (el) el.focus();
}

function closeSaleModal() {
    const elModal = document.getElementById('saleModal');
    if (elModal) elModal.classList.add('hidden');
    // Clear form
    const amountEl = document.getElementById('saleAmount');
    const profitEl = document.getElementById('saleProfit');
    const descEl = document.getElementById('saleDescription');
    if (amountEl) amountEl.value = '';
    if (profitEl) profitEl.value = '';
    if (descEl) descEl.value = '';
}

function openUtangModal() {
    const elModal = document.getElementById('utangModal');
    if (!elModal) return;
    elModal.classList.remove('hidden');
    const el = document.getElementById('utangCustomerName');
    if (el) el.focus();
}

function closeUtangModal() {
    const elModal = document.getElementById('utangModal');
    if (elModal) elModal.classList.add('hidden');
    // Clear form
    const nameEl = document.getElementById('utangCustomerName');
    const amountEl = document.getElementById('utangAmount');
    const dueEl = document.getElementById('utangDueDate');
    const profitEl = document.getElementById('utangProfit');
    if (nameEl) nameEl.value = '';
    if (amountEl) amountEl.value = '';
    if (dueEl) dueEl.value = '';
    if (profitEl) profitEl.value = '';
}

// Goal modal handlers
function openGoalModal() {
    const modal = document.getElementById('goalModal');
    if (!modal) return;
    const input = document.getElementById('dailyGoalInput');
    if (input) input.value = getDailyGoal();
    modal.classList.remove('hidden');
    if (input) input.focus();
}

function closeGoalModal() {
    const modal = document.getElementById('goalModal');
    if (!modal) return;
    modal.classList.add('hidden');
}

// Save daily goal from modal
async function saveDailyGoal() {
    const input = document.getElementById('dailyGoalInput');
    if (!input) return;
    const val = parseFloat(input.value);
    if (!isFinite(val) || val <= 0) {
        alert('Please enter a valid positive number for the daily goal.');
        input.focus();
        return;
    }
    try {
        await setDailyGoal(val);
        closeGoalModal();
        showNotification('Daily goal updated', 'success');

        // Update UI immediately
        if (typeof updateDashboard === 'function') updateDashboard();
    } catch (error) {
        console.error('Error saving daily goal:', error);
        alert('Unable to save daily goal. Please try again.');
    }
}

// Close modal when clicking outside
window.onclick = function (event) {
    const saleModal = document.getElementById('saleModal');
    const utangModal = document.getElementById('utangModal');
    const goalModal = document.getElementById('goalModal');

    if (event.target === saleModal) {
        closeSaleModal();
    }
    if (event.target === utangModal) {
        closeUtangModal();
    }
    if (event.target === goalModal) {
        closeGoalModal();
    }
}

// ==================== SALE FUNCTIONS ====================

async function addSale() {
    const amount = parseFloat(document.getElementById('saleAmount').value);
    const profit = parseFloat(document.getElementById('saleProfit').value);
    const description = document.getElementById('saleDescription').value;
    const productIdEl = document.getElementById('saleProductId');
    const qtyEl = document.getElementById('saleQty');
    const productId = productIdEl ? parseInt(productIdEl.value) : NaN;
    const qty = qtyEl ? parseInt(qtyEl.value) : NaN;

    if (!amount || isNaN(amount) || (!Number.isFinite(profit) && profit !== 0) || isNaN(profit)) {
        alert('Please fill in amount and profit fields');
        return;
    }

    // If productId and qty are present, attempt quick inventory sale
    try {
        if (Number.isFinite(productId) && productId > 0 && Number.isFinite(qty) && qty > 0) {
            await fetchJson('/api/sales/quick', {
                method: 'POST',
                body: JSON.stringify({ productId: productId, quantity: qty })
            });

            // Refresh sales and inventory UI
            await refreshSalesFromServer();
            closeSaleModal();
            if (typeof updateDashboard === 'function') updateDashboard();
            if (typeof updateSalesTracker === 'function') updateSalesTracker();
            showNotification('Sale recorded and inventory updated!', 'success');
            return;
        }

        const sale = {
            amount: amount,
            profit: profit,
            description: description || 'Sale'
        };

        const createdSale = await fetchJson('/api/sales', {
            method: 'POST',
            body: JSON.stringify(sale)
        });

        sales.unshift(createdSale);
        closeSaleModal();

        // Update UI based on current page
        if (typeof updateDashboard === 'function') updateDashboard();
        if (typeof updateSalesTracker === 'function') updateSalesTracker();

        showNotification('Sale added successfully!', 'success');
    } catch (error) {
        console.error('Error adding sale:', error);
        alert('Unable to add sale. Please try again.');
    }
}

function getSalesByPeriod(period) {
    const now = new Date();

    return sales.filter(sale => {
        const saleDate = new Date(sale.date);
        const diffTime = Math.abs(now - saleDate);
        const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

        if (period === 'daily') return diffDays <= 1;
        if (period === 'weekly') return diffDays <= 7;
        if (period === 'monthly') return diffDays <= 30;
        return true;
    });
}

// ==================== UTANG FUNCTIONS ====================

async function addUtang() {
    const customerNameEl = document.getElementById('utangCustomerName');
    const amountEl = document.getElementById('utangAmount');
    const dueDateEl = document.getElementById('utangDueDate');
    const profitEl = document.getElementById('utangProfit');

    const customerName = customerNameEl ? customerNameEl.value : '';
    const amount = amountEl ? parseFloat(amountEl.value) : NaN;
    const dueDate = dueDateEl ? dueDateEl.value : '';
    const profitInput = profitEl ? profitEl.value : '';
    const profit = profitInput === '' ? 0 : parseFloat(profitInput);

    if (!customerName || !amount || isNaN(amount) || !dueDate) {
        alert('Please fill in customer name, amount and due date');
        return;
    }

    if (profitInput !== '' && (isNaN(profit) || profit < 0)) {
        alert('Potential profit must be a positive number or left empty');
        return;
    }

    const utang = {
        customerName: customerName,
        amount: amount,
        profit: profit || 0,
        dueDate: dueDate
    };

    try {
        const response = await fetch('/api/utangs', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'same-origin',
            body: JSON.stringify(utang)
        });

        if (response.status === 401) {
            alert('You are not logged in. Please login to add utangs.');
            return;
        }

        if (response.ok) {
            const newUtang = await response.json();
            // Refresh data from server
            await refreshUtangsFromServer();
            closeUtangModal();
            
            // Update UI based on current page
            if (typeof updateDashboard === 'function') updateDashboard();
            if (typeof updateUtangLogs === 'function') updateUtangLogs();

            showNotification('Utang added successfully!', 'success');
        } else {
            alert('Failed to add utang. Please try again.');
        }
    } catch (error) {
        console.error('Error adding utang:', error);
        alert('Error adding utang. Please try again.');
    }
}

// ==================== UTANGS MANAGEMENT ====================

async function markUtangPaid(id) {
    try {
        // Confirm action
        if (!confirm('Are you sure you want to mark this utang as paid?')) {
            return;
        }

        // Find the utang to get its details
        const utang = utangs.find(u => u.id === id);
        if (!utang) {
            alert('Utang not found.');
            return;
        }

        // Delete utang from server
        const deleteResponse = await fetch(`/api/utangs/${id}`, {
            method: 'DELETE',
            credentials: 'same-origin'
        });

        if (deleteResponse.status === 401) {
            alert('You are not logged in.');
            return;
        }

        if (!deleteResponse.ok) {
            alert('Failed to mark utang as paid.');
            return;
        }

        // Utang deleted successfully - remove from local array
        utangs = utangs.filter(u => u.id !== id);

        // Create sale on server
        const saleData = {
            amount: parseFloat(utang.amount),
            profit: 0,
            description: `Payment from ${utang.customerName}`
        };

        const saleResponse = await fetch('/api/sales', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'same-origin',
            body: JSON.stringify(saleData)
        });

        if (saleResponse.ok) {
            const createdSale = await saleResponse.json();
            sales.unshift(createdSale);
        }

        // Update UI on current page only
        if (document.getElementById('utangTableContainer')) {
            renderUtangLogs();
        }

        showNotification('Utang marked as paid and added to sales!', 'success');
    } catch (error) {
        console.error('Error:', error);
        alert('Error: ' + error.message);
    }
}

function getUpcomingUtangs() {
    // Return all utangs sorted by due date
    return utangs.sort((a, b) => new Date(a.dueDate) - new Date(b.dueDate));
}

function getUtangStatus(dueDate) {
    const today = new Date();
    const due = new Date(dueDate);
    const daysUntilDue = Math.ceil((due - today) / (1000 * 60 * 60 * 24));

    if (daysUntilDue < 0) return { status: 'Overdue', class: 'bg-red-100 text-red-600' };
    if (daysUntilDue <= 3) return { status: 'Due Soon', class: 'bg-yellow-100 text-yellow-600' };
    return { status: 'Active', class: 'bg-green-100 text-green-600' };
}

// ==================== DASHBOARD UPDATES ====================

function updateDashboard() {
    Promise.all([
        refreshSalesFromServer(),
        refreshUtangsFromServer(),
        refreshFinancialSettingsFromServer()
    ])
        .then(() => {
            renderDashboard();
        })
        .catch((error) => {
            console.error('Error refreshing dashboard:', error);
            renderDashboard();
        });
}

function renderDashboard() {
    // Calculate totals
    const totalSales = sales.reduce((sum, sale) => sum + sale.amount, 0);
    const totalProfit = sales.reduce((sum, sale) => sum + sale.profit, 0);
    const dailyGoal = getDailyGoal();
    const progress = Math.min((totalSales / dailyGoal) * 100, 100);

    // Loss is now derived from outstanding utangs (sum of CURRENT amounts with aging)
    const totalLoss = utangs.reduce((sum, u) => sum + getCurrentAmount(u), 0);

    // Update stats cards
    const totalSalesCountEl = document.getElementById('totalSalesCount');
    const totalSalesAmountEl = document.getElementById('totalSalesAmount');
    const totalProfitEl = document.getElementById('totalProfit');

    if (totalSalesCountEl) totalSalesCountEl.textContent = sales.length;
    if (totalSalesAmountEl) totalSalesAmountEl.textContent = `₱${totalSales.toFixed(2)}`;
    if (totalProfitEl) totalProfitEl.textContent = `₱${totalProfit.toFixed(2)}`;

    // Update Loss card if present on page
    const lossEl = document.getElementById('totalLoss');
    if (lossEl) {
        lossEl.textContent = `₱${totalLoss.toFixed(2)}`;
    }

    // Update progress circle
    const circle = document.getElementById('progressCircle');
    if (circle) {
        const circumference = 2 * Math.PI * 88;
        const offset = circumference - (progress / 100) * circumference;
        circle.style.strokeDashoffset = offset;
    }

    const percentEl = document.getElementById('progressPercent');
    if (percentEl) percentEl.textContent = `${Math.round(progress)}%`;
    const progressText = document.getElementById('progressText');
    if (progressText) progressText.textContent = `₱${totalSales.toFixed(0)} / ₱${Number(dailyGoal).toFixed(0)}`;

    // Update upcoming utangs (show current amount with aging)
    const upcomingUtangs = getUpcomingUtangs();
    const utangContainer = document.getElementById('upcomingUtangs');

    if (utangContainer) {
        if (upcomingUtangs.length === 0) {
            utangContainer.innerHTML = '<p class="text-gray-400 text-center py-8">No upcoming utangs</p>';
        } else {
            utangContainer.innerHTML = upcomingUtangs.map(utang => {
                const currentAmount = getCurrentAmount(utang);
                const penalty = getPenaltyAmount(utang);
                const hasAging = penalty > 0;

                return `
                <div class="flex justify-between items-center p-3 bg-orange-50 rounded-lg mb-3">
                    <div>
                        <p class="font-semibold text-gray-800">${utang.customerName}</p>
                        <p class="text-sm text-gray-500">
                            ${hasAging ?
                        `<span class="line-through">₱${utang.amount.toFixed(2)}</span> 
                                 <span class="text-red-600 font-bold">₱${currentAmount.toFixed(2)}</span>` :
                        `₱${utang.amount.toFixed(2)}`
                    }
                        </p>
                        ${hasAging ? `<p class="text-xs text-red-500">+₱${penalty.toFixed(2)} penalty</p>` : ''}
                    </div>
                    <div class="text-right">
                        <p class="text-sm text-red-500 font-medium">Due</p>
                        <p class="text-xs text-gray-500">${new Date(utang.dueDate).toLocaleDateString()}</p>
                    </div>
                </div>
                `;
            }).join('');
        }
    }

    // Update recent sales
    const recentSales = sales.slice(-5).reverse();
    const recentContainer = document.getElementById('recentSalesContainer');
    const recentList = document.getElementById('recentSalesList');

    if (recentList) {
        if (recentSales.length > 0) {
            if (recentContainer) recentContainer.style.display = 'block';
            recentList.innerHTML = recentSales.map(sale => `
                <div class="flex justify-between items-center p-3 border-b border-gray-100">
                    <div>
                        <p class="font-semibold text-gray-800">${sale.description}</p>
                        <p class="text-sm text-gray-500">₱${sale.amount.toFixed(2)}</p>
                    </div>
                    <p class="text-green-500 font-semibold">+₱${sale.profit.toFixed(2)}</p>
                </div>
            `).join('');
        } else {
            if (recentContainer) recentContainer.style.display = 'none';
        }
    }
}

// ==================== SALES TRACKER UPDATES ====================

async function updateSalesTracker() {
    try {
        await refreshSalesFromServer();
    } catch (error) {
        console.error('Error refreshing sales:', error);
    }

    // Update period stats
    const dailySales = getSalesByPeriod('daily');
    const weeklySales = getSalesByPeriod('weekly');
    const monthlySales = getSalesByPeriod('monthly');

    // Daily stats
    const dailyTotal = dailySales.reduce((sum, s) => sum + s.amount, 0);
    const dailyProfit = dailySales.reduce((sum, s) => sum + s.profit, 0);
    const dailySalesEl = document.getElementById('dailySales');
    const dailyProfitEl = document.getElementById('dailyProfit');
    const dailyCountEl = document.getElementById('dailyCount');
    if (dailySalesEl) dailySalesEl.textContent = `₱${dailyTotal.toFixed(2)}`;
    if (dailyProfitEl) dailyProfitEl.textContent = `Profit: ₱${dailyProfit.toFixed(2)}`;
    if (dailyCountEl) dailyCountEl.textContent = `${dailySales.length} sales`;

    // Weekly stats
    const weeklyTotal = weeklySales.reduce((sum, s) => sum + s.amount, 0);
    const weeklyProfit = weeklySales.reduce((sum, s) => sum + s.profit, 0);
    const weeklySalesEl = document.getElementById('weeklySales');
    const weeklyProfitEl = document.getElementById('weeklyProfit');
    const weeklyCountEl = document.getElementById('weeklyCount');
    if (weeklySalesEl) weeklySalesEl.textContent = `₱${weeklyTotal.toFixed(2)}`;
    if (weeklyProfitEl) weeklyProfitEl.textContent = `Profit: ₱${weeklyProfit.toFixed(2)}`;
    if (weeklyCountEl) weeklyCountEl.textContent = `${weeklySales.length} sales`;

    // Monthly stats
    const monthlyTotal = monthlySales.reduce((sum, s) => sum + s.amount, 0);
    const monthlyProfit = monthlySales.reduce((sum, s) => sum + s.profit, 0);
    const monthlySalesEl = document.getElementById('monthlySales');
    const monthlyProfitEl = document.getElementById('monthlyProfit');
    const monthlyCountEl = document.getElementById('monthlyCount');
    if (monthlySalesEl) monthlySalesEl.textContent = `₱${monthlyTotal.toFixed(2)}`;
    if (monthlyProfitEl) monthlyProfitEl.textContent = `Profit: ₱${monthlyProfit.toFixed(2)}`;
    if (monthlyCountEl) monthlyCountEl.textContent = `${monthlySales.length} sales`;

    // Update sales history table
    const tableContainer = document.getElementById('salesHistoryTable');

    if (tableContainer) {
        if (sales.length === 0) {
            tableContainer.innerHTML = `<p class="text-gray-400 text-center py-12">No sales recorded yet</p>`;
        } else {
            const sortedSales = [...sales].reverse();
            tableContainer.innerHTML = `
                <div class="overflow-x-auto">
                    <table class="w-full">
                        <thead>
                            <tr class="border-b border-gray-200">
                                <th class="text-left py-3 px-4 text-gray-600 font-semibold">Date</th>
                                <th class="text-left py-3 px-4 text-gray-600 font-semibold">Description</th>
                                <th class="text-right py-3 px-4 text-gray-600 font-semibold">Amount</th>
                                <th class="text-right py-3 px-4 text-gray-600 font-semibold">Profit</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${sortedSales.map(sale => `
                                <tr class="border-b border-gray-100 hover:bg-gray-50">
                                    <td class="py-3 px-4 text-gray-700">${new Date(sale.date).toLocaleDateString()}</td>
                                    <td class="py-3 px-4 text-gray-700">${sale.description}</td>
                                    <td class="py-3 px-4 text-right font-semibold text-gray-800">₱${sale.amount.toFixed(2)}</td>
                                    <td class="py-3 px-4 text-right font-semibold text-green-500">₱${sale.profit.toFixed(2)}</td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>
            `;
        }
    }
}

// ==================== UTANG LOGS UPDATES ====================

function updateUtangLogs() {
    refreshDataFromStorage();
    
    // Try to fetch from server, fallback to localStorage
    fetch('/api/utangs', { credentials: 'same-origin' })
        .then(response => {
            if (response.status === 401) {
                console.warn('Not authenticated when fetching utang logs (server returned 401).');
                return utangs;
            }
            return response.ok ? response.json() : utangs;
        })
        .then(serverUtangs => {
            if (Array.isArray(serverUtangs)) {
                utangs = serverUtangs;
                saveUtangs();
            }
            renderUtangLogs();
        })
        .catch(() => {
            // Fallback to localStorage data
            renderUtangLogs();
        });
}

function renderUtangLogs() {
    const tableContainer = document.getElementById('utangTableContainer');

    if (!tableContainer) return;

    if (utangs.length === 0) {
        tableContainer.innerHTML = `
            <div class="bg-white rounded-xl p-12 shadow-md text-center">
                <svg class="w-16 h-16 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
                </svg>
                <p class="text-gray-400 text-lg">No utang records yet</p>
                <p class="text-gray-400 text-sm mt-2">Click "Add Utang" to start tracking</p>
            </div>
        `;
    } else {
        const sortedUtangs = [...utangs].sort((a, b) => new Date(a.dueDate) - new Date(b.dueDate));

        tableContainer.innerHTML = `
            <div class="bg-white rounded-xl shadow-md overflow-hidden">
                <div class="overflow-x-auto">
                <table class="w-full table-auto">
                    <colgroup>
                        <col style="width:22%">
                        <col style="width:13%">
                        <col style="width:13%">
                        <col style="width:13%">
                        <col style="width:13%">
                        <col style="width:13%">
                        <col style="width:13%">
                    </colgroup>
                    <thead class="bg-orange-500 text-white">
                        <tr>
                            <th class="text-left py-4 px-6 font-semibold">Customer Name</th>
                            <th class="text-right py-4 px-6 font-semibold">Original Amount</th>
                            <th class="text-right py-4 px-6 font-semibold">Current Amount</th>
                            <th class="text-right py-4 px-6 font-semibold">Penalty</th>
                            <th class="text-center py-4 px-6 font-semibold">Due Date</th>
                            <th class="text-center py-4 px-6 font-semibold">Status</th>
                            <th class="text-center py-4 px-6 font-semibold">Action</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${sortedUtangs.map(utang => {
            const status = getUtangStatus(utang.dueDate);
            const currentAmount = getCurrentAmount(utang);
            const penalty = getPenaltyAmount(utang);
            const monthsOverdue = getMonthsOverdue(utang.dueDate);
            const hasAging = penalty > 0;

            return `
                                <tr class="border-b border-gray-100 hover:bg-gray-50">
                                    <td class="py-4 px-6 font-semibold text-gray-800">${utang.customerName}</td>
                                    <td class="py-4 px-6 text-right text-gray-600">₱${utang.amount.toFixed(2)}</td>
                                    <td class="py-4 px-6 text-right font-bold ${hasAging ? 'text-red-600' : 'text-gray-800'}">
                                        ₱${currentAmount.toFixed(2)}
                                        ${hasAging ? `<span class="text-xs block text-red-500">(${monthsOverdue} mo. overdue)</span>` : ''}
                                    </td>
                                    <td class="py-4 px-6 text-right ${hasAging ? 'text-red-600 font-bold' : 'text-gray-500'}">
                                        ${hasAging ? `+₱${penalty.toFixed(2)}` : '₱0.00'}
                                    </td>
                                    <td class="py-4 px-6 text-center text-gray-600">${new Date(utang.dueDate).toLocaleDateString()}</td>
                                    <td class="py-4 px-6 text-center">
                                        <span class="px-3 py-1 rounded-full text-sm font-semibold ${status.class}">
                                            ${status.status}
                                        </span>
                                    </td>
                                    <td class="py-4 px-6 text-center">
                                        <button onclick="markUtangPaid(${utang.id})" 
                                                class="inline-block whitespace-nowrap bg-green-500 text-white px-4 py-2 rounded-lg text-sm font-semibold hover:bg-green-600 transition-colors">
                                            Mark Paid
                                        </button>
                                    </td>
                                </tr>
                            `;
        }).join('')}
                    </tbody>
                </table>
                </div>
            </div>
        `;
    }

    // Update summary (use current amounts with aging)
    const totalAmount = utangs.reduce((sum, u) => sum + getCurrentAmount(u), 0);
    const dueSoonCount = getUpcomingUtangs().length;

    const totalUtangCountEl = document.getElementById('totalUtangCount');
    const totalUtangAmountEl = document.getElementById('totalUtangAmount');
    const dueSoonCountEl = document.getElementById('dueSoonCount');

    if (totalUtangCountEl) totalUtangCountEl.textContent = utangs.length;
    if (totalUtangAmountEl) totalUtangAmountEl.textContent = `₱${totalAmount.toFixed(2)}`;
    if (dueSoonCountEl) dueSoonCountEl.textContent = dueSoonCount;
}

// ==================== EXPENSES & KNAPSACK ALGORITHM ====================



// New function: clear expenses and budget so user can start fresh
function saveExpenses() {
    notifyDataChanged();
}

function saveBudget() {
    saveFinancialSettings().catch((error) => console.error('Error saving budget:', error));
}

async function clearExpensesAndBudget() {
    const shouldClearExpenses = confirm('Clear ALL expenses and reset the budget to zero? This cannot be undone.');
    if (!shouldClearExpenses) return;

    try {
        await fetchJson('/api/expenses', { method: 'DELETE' });
    } catch (error) {
        console.error('Error clearing expenses:', error);
        alert('Unable to clear expenses. Please try again.');
        return;
    }
    // confirmed before the server delete

    // Clear in-memory
    expenses = [];
    availableBudget = 0;

    // Persist budget reset
    await saveFinancialSettings();

    // Update UI immediately
    if (typeof updateExpensesPage === 'function') updateExpensesPage();
    if (typeof updateDashboard === 'function') updateDashboard();

    showNotification('Expenses cleared and budget reset to ₱0', 'success');
}

// ==================== KNAPSACK OPTIMIZATION ====================

// ==================== UI HELPERS ====================

function getPriorityStars(priority) {
    return '★'.repeat(priority) + '☆'.repeat(5 - priority);
}

function getDaysUntilDue(dueDate) {
    const today = new Date();
    const due = new Date(dueDate);
    today.setHours(0, 0, 0, 0);
    due.setHours(0, 0, 0, 0);
    const diffTime = due - today;
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
}

function getUrgencyClass(days) {
    if (days < 0) return 'text-red-600 bg-red-100';
    if (days <= 3) return 'text-orange-600 bg-orange-100';
    if (days <= 7) return 'text-yellow-600 bg-yellow-100';
    return 'text-green-600 bg-green-100';
}

// ==================== EXPENSE MODAL ====================

function openExpenseModal() {
    currentExpenseEditingId = null;
    clearValidationErrors();
    resetExpenseForm();
    
    document.getElementById('expenseModalTitle').textContent = 'Add Expense';
    document.getElementById('expenseSaveBtn').textContent = 'Save Expense';
    document.getElementById('expenseErrorAlert').classList.add('hidden');
    
    const modal = document.getElementById('expenseModal');
    if (!modal) return;
    modal.classList.remove('hidden');
    const nameEl = document.getElementById('expenseName');
    if (nameEl) nameEl.focus();
}

function editExpense(id) {
    const expense = expenses.find(e => e.id === id);
    if (!expense) return;

    currentExpenseEditingId = id;
    clearValidationErrors();
    
    // Populate form
    document.getElementById('expenseName').value = expense.name || '';
    document.getElementById('expenseAmount').value = expense.amount.toFixed(2);
    document.getElementById('expenseDueDate').value = new Date(expense.dueDate).toISOString().split('T')[0];
    document.getElementById('expensePriority').value = expense.priority;
    
    updatePriorityDisplay(expense.priority);
    
    document.getElementById('expenseModalTitle').textContent = 'Edit Expense';
    document.getElementById('expenseSaveBtn').textContent = 'Update Expense';
    document.getElementById('expenseErrorAlert').classList.add('hidden');
    
    const modal = document.getElementById('expenseModal');
    if (!modal) return;
    modal.classList.remove('hidden');
}

function closeExpenseModal() {
    const modal = document.getElementById('expenseModal');
    if (!modal) return;
    modal.classList.add('hidden');
    resetExpenseForm();
    clearValidationErrors();
    currentExpenseEditingId = null;
}

function resetExpenseForm() {
    document.getElementById('expenseName').value = '';
    document.getElementById('expenseAmount').value = '';
    document.getElementById('expenseDueDate').value = '';
    document.getElementById('expensePriority').value = '3';
    updatePriorityDisplay(3);
}

function clearValidationErrors() {
    document.querySelectorAll('[id$="Error"]').forEach(el => {
        if (el.id !== 'expenseErrorAlert') el.classList.add('hidden');
    });
}

function validateExpense(name, amount, dueDate) {
    clearValidationErrors();
    let isValid = true;
    
    if (!name || name.trim() === '') {
        document.getElementById('nameError').classList.remove('hidden');
        isValid = false;
    }
    
    if (!amount || isNaN(amount) || amount <= 0) {
        document.getElementById('amountError').classList.remove('hidden');
        isValid = false;
    }
    
    if (!dueDate) {
        document.getElementById('dueDateError').classList.remove('hidden');
        isValid = false;
        return isValid;
    }
    
    // Check if due date is in the past
    const selectedDate = new Date(dueDate);
    selectedDate.setHours(0, 0, 0, 0);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    
    if (selectedDate < today) {
        document.getElementById('dueDateError').textContent = 'Due date cannot be in the past';
        document.getElementById('dueDateError').classList.remove('hidden');
        isValid = false;
    }
    
    return isValid;
}

function updatePriorityDisplay(priority) {
    const stars = getPriorityStars(parseInt(priority));
    const disp = document.getElementById('priorityDisplay');
    if (disp) disp.textContent = `${stars} (${priority}/5)`;
}

// ==================== EXPENSE ACTIONS ====================

async function saveExpense() {
    const nameEl = document.getElementById('expenseName');
    const amountEl = document.getElementById('expenseAmount');
    const dueDateEl = document.getElementById('expenseDueDate');
    const prEl = document.getElementById('expensePriority');

    const name = nameEl ? nameEl.value.trim() : '';
    const amount = amountEl ? parseFloat(amountEl.value) : NaN;
    const dueDate = dueDateEl ? dueDateEl.value : '';
    const priority = prEl ? parseInt(prEl.value) : 3;

    // Validate
    if (!validateExpense(name, amount, dueDate)) {
        return;
    }

    const expenseData = {
        name,
        amount,
        dueDate,
        priority,
        paid: false
    };

    try {
        if (currentExpenseEditingId) {
            // Update existing expense
            await fetchJson(`/api/expenses/${currentExpenseEditingId}`, {
                method: 'PUT',
                body: JSON.stringify(expenseData)
            });
            showNotification('Expense updated successfully!', 'success');
        } else {
            // Create new expense
            const createdExpense = await fetchJson('/api/expenses', {
                method: 'POST',
                body: JSON.stringify(expenseData)
            });
            expenses.push(createdExpense);
            showNotification('Expense added successfully!', 'success');
        }
        
        closeExpenseModal();
        currentPage = 1; // Reset to first page
        await updateExpensesPage();
    } catch (error) {
        console.error('Error saving expense:', error);
        const errorAlert = document.getElementById('expenseErrorAlert');
        const errorText = document.getElementById('expenseErrorText');
        if (errorAlert && errorText) {
            errorText.textContent = 'Unable to save expense. Please try again.';
            errorAlert.classList.remove('hidden');
        }
    }
}

async function markExpensePaid(id) {
    // Ensure latest state
    await Promise.all([refreshExpensesFromServer(), refreshFinancialSettingsFromServer()]);

    const idx = expenses.findIndex(e => e.id === id);
    if (idx === -1) return;

    const expense = expenses[idx];

    // If already paid, do nothing
    if (expense.paid) {
        showNotification('Expense already marked as paid', 'success');
        return;
    }

    // Update the expense to mark as paid
    const updatedExpense = { ...expense, paid: true };

    try {
        await fetchJson(`/api/expenses/${id}`, {
            method: 'PUT',
            body: JSON.stringify(updatedExpense)
        });
        
        // Subtract from budget
        const amt = parseFloat(expense.amount) || 0;
        availableBudget = Math.max(0, availableBudget - amt);
        expenses[idx] = updatedExpense;
        
        await saveFinancialSettings();
        updateExpensesPage();
        showNotification(`Expense marked as paid. Budget reduced by ₱${amt.toFixed(2)}.`, 'success');
    } catch (error) {
        console.error('Error marking expense paid:', error);
        showNotification('Unable to mark expense as paid. Please try again.', 'error');
    }
}

async function deleteExpense(id) {
    if (!confirm('Delete this expense?')) return;

    try {
        await fetchJson(`/api/expenses/${id}`, { method: 'DELETE' });
        expenses = expenses.filter(expense => expense.id !== id);
        currentPage = 1; // Reset to first page
        await updateExpensesPage();
        showNotification('Expense deleted!', 'success');
    } catch (error) {
        console.error('Error deleting expense:', error);
        showNotification('Unable to delete expense. Please try again.', 'error');
    }
}

async function updateBudget() {
    const newBudget = prompt('Enter new available budget:', availableBudget);

    if (newBudget && !isNaN(newBudget) && parseFloat(newBudget) >= 0) {
        availableBudget = parseFloat(newBudget);
        await saveFinancialSettings();
        await updateExpensesPage();
        showNotification('Budget updated!', 'success');
    } else if (newBudget !== null) {
        showNotification('Invalid budget amount', 'error');
    }
}

// ==================== PAGE RENDERING ====================

async function updateExpensesPage() {
    try {
        await Promise.all([refreshExpensesFromServer(), refreshFinancialSettingsFromServer()]);
    } catch (error) {
        console.error('Error refreshing expenses:', error);
    }

    // Get filter status
    const filterSelect = document.getElementById('expenseFilterStatus');
    const filterStatus = filterSelect ? filterSelect.value : 'all';
    
    // Filter expenses
    let filteredExpenses = [...expenses];
    
    if (filterStatus === 'unpaid') {
        filteredExpenses = filteredExpenses.filter(e => !e.paid);
    } else if (filterStatus === 'paid') {
        filteredExpenses = filteredExpenses.filter(e => e.paid);
    } else if (filterStatus === 'overdue') {
        filteredExpenses = filteredExpenses.filter(e => {
            const daysUntilDue = getDaysUntilDue(e.dueDate);
            return daysUntilDue < 0 && !e.paid;
        });
    }

    // Call server-side optimization endpoint
    let recommendedIds = [];
    try {
        const optimizationResult = await fetchJson(`/api/expenses/optimize?budget=${availableBudget}`, {
            method: 'GET'
        });
        
        if (optimizationResult && optimizationResult.recommendedExpenses) {
            recommendedIds = optimizationResult.recommendedExpenses.map(e => e.id);
        }
    } catch (error) {
        console.error('Error getting expense optimization:', error);
    }

    // Calculate stats
    const totalUnpaid = expenses
        .filter(e => !e.paid)
        .reduce((sum, e) => sum + e.amount, 0);

    const optimizedTotal = expenses
        .filter(e => recommendedIds.includes(e.id))
        .reduce((sum, e) => sum + e.amount, 0);

    const paidCount = expenses.filter(e => e.paid).length;
    const unpaidCount = expenses.filter(e => !e.paid).length;
    const overdueCount = expenses.filter(e => getDaysUntilDue(e.dueDate) < 0 && !e.paid).length;

    // Update stats display
    updateStatsDisplay(totalUnpaid, optimizedTotal, unpaidCount, paidCount, overdueCount);

    // Update overdue alert
    const overdueAlert = document.getElementById('overdueAlert');
    const overdueCountEl = document.getElementById('overdueCount');
    if (overdueAlert) {
        if (overdueCount > 0) {
            overdueAlert.classList.remove('hidden');
            if (overdueCountEl) overdueCountEl.textContent = overdueCount;
        } else {
            overdueAlert.classList.add('hidden');
        }
    }

    // Render the expenses table
    renderExpensesTable(filteredExpenses, recommendedIds);
}

function updateStatsDisplay(totalUnpaid, optimizedTotal, unpaidCount, paidCount, overdueCount) {
    const availableBudgetEl = document.getElementById('availableBudget');
    if (availableBudgetEl) availableBudgetEl.textContent = `₱${availableBudget.toFixed(2)}`;

    const totalUnpaidEl = document.getElementById('totalUnpaid');
    const optimizedCostEl = document.getElementById('optimizedCost');
    const unpaidCountEl = document.getElementById('unpaidCount');
    const paidCountEl = document.getElementById('paidCount');

    if (totalUnpaidEl) totalUnpaidEl.textContent = `₱${totalUnpaid.toFixed(2)}`;
    if (optimizedCostEl) optimizedCostEl.textContent = `₱${optimizedTotal.toFixed(2)}`;
    if (unpaidCountEl) unpaidCountEl.textContent = unpaidCount;
    if (paidCountEl) paidCountEl.textContent = paidCount;
}

// ==================== UPDATE EXPENSES TABLE WITH PAGINATION ====================

function renderExpensesTable(filteredExpenses, recommendedIds = []) {
    const tableContainer = document.getElementById('expensesTableContainer');
    if (!tableContainer) return;

    if (!Array.isArray(filteredExpenses) || filteredExpenses.length === 0) {
        tableContainer.innerHTML = `
        <div class="p-12 text-center">
            <svg class="w-16 h-16 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M9 14l6-6m-5.5.5h.01m4.99 5h.01M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16l3.5-2 3.5 2 3.5-2 3.5 2zM10 8.5a.5.5 0 11-1 0 .5.5 0 011 0zm5 5a.5.5 0 11-1 0 .5.5 0 011 0z">
                </path>
            </svg>
            <p class="text-gray-400 text-lg">No expenses found</p>
        </div>
    `;
        hidePagination();
        return;
    }

    // Sort expenses
    const sortedExpenses = [...filteredExpenses].sort((a, b) => {
        // Unpaid items first
        if (a.paid !== b.paid) return a.paid ? 1 : -1;
        // Unpaid: higher priority first
        if (!a.paid && !b.paid) {
            if (a.priority !== b.priority) return b.priority - a.priority;
            return new Date(a.dueDate) - new Date(b.dueDate);
        }
        return new Date(a.dueDate) - new Date(b.dueDate);
    });

    // Pagination
    const totalPages = Math.ceil(sortedExpenses.length / pageSize);
    if (currentPage > totalPages) currentPage = Math.max(1, totalPages);
    
    const startIdx = (currentPage - 1) * pageSize;
    const endIdx = startIdx + pageSize;
    const pageExpenses = sortedExpenses.slice(startIdx, endIdx);

    // Render table
    tableContainer.innerHTML = `
        <div class="overflow-x-auto">
            <table class="w-full">
                <thead class="bg-gray-50">
                    <tr>
                        <th class="text-left py-4 px-6 text-gray-600 font-semibold">Expense Name</th>
                        <th class="text-right py-4 px-6 text-gray-600 font-semibold">Amount</th>
                        <th class="text-center py-4 px-6 text-gray-600 font-semibold">Due Date</th>
                        <th class="text-center py-4 px-6 text-gray-600 font-semibold">Priority</th>
                        <th class="text-center py-4 px-6 text-gray-600 font-semibold">Recommended</th>
                        <th class="text-center py-4 px-6 text-gray-600 font-semibold">Status</th>
                        <th class="text-center py-4 px-6 text-gray-600 font-semibold">Actions</th>
                    </tr>
                </thead>
                <tbody>
                    ${pageExpenses.map(expense => {
        const daysUntilDue = getDaysUntilDue(expense.dueDate);
        const isRecommended = recommendedIds.includes(expense.id);
        const urgencyClass = getUrgencyClass(daysUntilDue);
        const isOverdue = daysUntilDue < 0 && !expense.paid;

        // Row highlighting
        const rowClasses = ['border-b', 'border-gray-100'];
        if (isOverdue) rowClasses.push('bg-red-50');
        else if (!expense.paid) rowClasses.push('bg-yellow-50');
        if (isRecommended && !expense.paid) rowClasses.push('bg-blue-50');

        return `
                            <tr class="${rowClasses.join(' ')}">
                                <td class="py-4 px-6">
                                    <div class="flex items-center gap-2">
                                        ${expense.paid
                ? `<svg class="w-5 h-5 text-green-500" fill="currentColor" viewBox="0 0 24 24">
                                               <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41L9 16.17z"/>
                                           </svg>`
                : ''
            }
                                        <span class="font-semibold ${expense.paid ? 'text-gray-400 line-through' : 'text-gray-800'}">
                                            ${expense.name}
                                        </span>
                                        ${isOverdue ? '<span class="text-red-600 font-bold ml-2">OVERDUE</span>' : ''}
                                    </div>
                                </td>
                                <td class="py-4 px-6 text-right font-semibold text-gray-800">
                                    ₱${expense.amount.toFixed(2)}
                                </td>
                                <td class="py-4 px-6 text-center">
                                    <p class="text-gray-800">${new Date(expense.dueDate).toLocaleDateString()}</p>
                                    ${!expense.paid
                ? `<p class="text-xs font-semibold ${urgencyClass} px-2 py-1 rounded inline-block mt-1">
                                           ${daysUntilDue < 0 ? `${Math.abs(daysUntilDue)} days overdue` : `${daysUntilDue} days left`}
                                       </p>`
                : ''
            }
                                </td>
                                <td class="py-4 px-6 text-center">
                                    <span class="text-yellow-500 text-lg">${getPriorityStars(expense.priority)}</span>
                                </td>
                                <td class="py-4 px-6 text-center">
                                    ${!expense.paid && isRecommended
                ? '<span class="bg-blue-500 text-white px-3 py-1 rounded-full text-sm font-semibold">✓ Yes</span>'
                : !expense.paid
                    ? '<span class="text-gray-400 text-sm">Not in budget</span>'
                    : '<span class="text-gray-400 text-sm">—</span>'
            }
                                </td>
                                <td class="py-4 px-6 text-center">
                                    <span class="px-3 py-1 rounded-full text-sm font-semibold ${expense.paid
                ? 'bg-green-100 text-green-600'
                : 'bg-red-100 text-red-600'
            }">
                                        ${expense.paid ? 'Paid' : 'Unpaid'}
                                    </span>
                                </td>
                                <td class="py-4 px-6 text-center">
                                    <div class="flex gap-1 justify-center flex-wrap">
                                        ${!expense.paid
                ? `<button onclick="editExpense(${expense.id})"
                                               class="bg-blue-500 text-white px-2 py-1 rounded text-sm font-semibold hover:bg-blue-600">
                                               Edit
                                           </button>
                                           <button onclick="markExpensePaid(${expense.id})"
                                               class="bg-green-500 text-white px-2 py-1 rounded text-sm font-semibold hover:bg-green-600">
                                               Mark Paid
                                           </button>`
                : ''
            }
                                        <button onclick="deleteExpense(${expense.id})"
                                            class="bg-red-500 text-white px-2 py-1 rounded text-sm font-semibold hover:bg-red-600">
                                            Delete
                                        </button>
                                    </div>
                                </td>
                            </tr>
                        `;
    }).join('')}
                </tbody>
            </table>
        </div>
    `;

    // Show/update pagination
    if (totalPages > 1) {
        showPagination(currentPage, totalPages);
    } else {
        hidePagination();
    }

    // Render recommendation card
    renderRecommendationCard(recommendedIds, availableBudget);
}

function showPagination(current, total) {
    const paginationContainer = document.getElementById('paginationContainer');
    if (!paginationContainer) return;
    
    paginationContainer.classList.remove('hidden');
    const pageInfo = document.getElementById('pageInfo');
    if (pageInfo) pageInfo.textContent = `Page ${current} of ${total}`;
}

function hidePagination() {
    const paginationContainer = document.getElementById('paginationContainer');
    if (paginationContainer) paginationContainer.classList.add('hidden');
}

function nextPage() {
    const filterSelect = document.getElementById('expenseFilterStatus');
    const filterStatus = filterSelect ? filterSelect.value : 'all';
    
    let filteredExpenses = [...expenses];
    if (filterStatus === 'unpaid') {
        filteredExpenses = filteredExpenses.filter(e => !e.paid);
    } else if (filterStatus === 'paid') {
        filteredExpenses = filteredExpenses.filter(e => e.paid);
    } else if (filterStatus === 'overdue') {
        filteredExpenses = filteredExpenses.filter(e => {
            const daysUntilDue = getDaysUntilDue(e.dueDate);
            return daysUntilDue < 0 && !e.paid;
        });
    }
    
    const totalPages = Math.ceil(filteredExpenses.length / pageSize);
    if (currentPage < totalPages) {
        currentPage++;
        updateExpensesPage();
    }
}

function previousPage() {
    if (currentPage > 1) {
        currentPage--;
        updateExpensesPage();
    }
}

function renderRecommendationCard(recommendedIds, budget) {
    const aiRecommendation = document.getElementById('aiRecommendation');
    if (!aiRecommendation) return;

    const recommendedExpenses = expenses.filter(e => recommendedIds.includes(e.id));
    const optimizedTotal = recommendedExpenses.reduce((sum, e) => sum + e.amount, 0);

    aiRecommendation.innerHTML = `
        <div class="flex items-start gap-4">
            <div class="bg-white/20 p-3 rounded-full">
                <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                </svg>
            </div>
            <div class="flex-1">
                <h3 class="text-2xl font-bold mb-2">Expense Recommendation</h3>
                <p class="text-blue-100 mb-4">
                    Based on priority and urgency, here's the optimal payment strategy with your budget of ₱${budget.toFixed(2)}:
                </p>
                <div class="bg-white/10 rounded-xl p-4">
                    <p class="font-semibold mb-2">Recommended Expenses to Pay:</p>
                    ${recommendedExpenses.length === 0
        ? '<p class="text-blue-100">Your budget is insufficient for any unpaid expenses.</p>'
        : `<ul class="space-y-2">
                        ${recommendedExpenses.map(e => `
                            <li class="flex justify-between">
                                <span>✓ ${e.name}</span>
                                <span class="font-semibold">₱${e.amount.toFixed(2)}</span>
                            </li>
                        `).join('')}
                       </ul>`
    }
                    <div class="mt-4 pt-4 border-t border-white/20">
                        <div class="flex justify-between font-semibold">
                            <span>Total Cost:</span>
                            <span>₱${optimizedTotal.toFixed(2)}</span>
                        </div>
                        <div class="flex justify-between text-blue-100 text-sm mt-1">
                            <span>Remaining Budget:</span>
                            <span>₱${(budget - optimizedTotal).toFixed(2)}</span>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `;
}

// ==================== RENDER EXPENSES TABLE WITH PAGINATION ====================

function renderExpensesTable(filteredExpenses, recommendedIds = []) {
    const tableContainer = document.getElementById('expensesTableContainer');
    if (!tableContainer) return;

    if (!Array.isArray(expenses) || expenses.length === 0) {
        tableContainer.innerHTML = `
        <div class="p-12 text-center">
            <svg class="w-16 h-16 mx-auto text-gray-300 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M9 14l6-6m-5.5.5h.01m4.99 5h.01M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16l3.5-2 3.5 2 3.5-2 3.5 2zM10 8.5a.5.5 0 11-1 0 .5.5 0 011 0zm5 5a.5.5 0 11-1 0 .5.5 0 011 0z">
                </path>
            </svg>
            <p class="text-gray-400 text-lg">No expenses recorded yet</p>
        </div>
    `;
        return;
    }

    tableContainer.innerHTML = `
        <div class="overflow-x-auto">
            <table class="w-full">
                <thead class="bg-gray-50">
                    <tr>
                        <th class="text-left py-4 px-6 text-gray-600 font-semibold">Expense Name</th>
                        <th class="text-right py-4 px-6 text-gray-600 font-semibold">Amount</th>
                        <th class="text-center py-4 px-6 text-gray-600 font-semibold">Due Date</th>
                        <th class="text-center py-4 px-6 text-gray-600 font-semibold">Priority</th>
                        <th class="text-center py-4 px-6 text-gray-600 font-semibold">Recommended</th>
                        <th class="text-center py-4 px-6 text-gray-600 font-semibold">Status</th>
                        <th class="text-center py-4 px-6 text-gray-600 font-semibold">Actions</th>
                    </tr>
                </thead>
                <tbody>
                    ${filteredExpenses.map(expense => {
        const daysUntilDue = getDaysUntilDue(expense.dueDate);
        const isRecommended = recommendedIds.includes(expense.id);
        const urgencyClass = getUrgencyClass(daysUntilDue);
        const isOverdue = daysUntilDue < 0 && !expense.paid;

        const rowClasses = ['border-b', 'border-gray-100'];
        if (isOverdue) rowClasses.push('bg-red-50');
        else if (!expense.paid) rowClasses.push('bg-yellow-50');
        if (isRecommended && !expense.paid) rowClasses.push('bg-blue-50');

        return `
                            <tr class="${rowClasses.join(' ')}">
                                <td class="py-4 px-6">
                                    <div class="flex items-center gap-2">
                                        ${expense.paid
                ? `<svg class="w-5 h-5 text-green-500" fill="currentColor" viewBox="0 0 24 24">
                                               <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41L9 16.17z"/>
                                           </svg>`
                : ''
            }
                                        <span class="font-semibold ${expense.paid ? 'text-gray-400 line-through' : 'text-gray-800'}">
                                            ${expense.name}
                                        </span>
                                        ${isOverdue ? '<span class="text-red-600 font-bold ml-2">OVERDUE</span>' : ''}
                                    </div>
                                </td>
                                <td class="py-4 px-6 text-right font-semibold text-gray-800">
                                    ₱${expense.amount.toFixed(2)}
                                </td>
                                <td class="py-4 px-6 text-center">
                                    <p class="text-gray-800">${new Date(expense.dueDate).toLocaleDateString()}</p>
                                    ${!expense.paid
                ? `<p class="text-xs font-semibold ${urgencyClass} px-2 py-1 rounded inline-block mt-1">
                                           ${daysUntilDue < 0 ? `${Math.abs(daysUntilDue)} days overdue` : `${daysUntilDue} days left`}
                                       </p>`
                : ''
            }
                                </td>
                                <td class="py-4 px-6 text-center">
                                    <span class="text-yellow-500 text-lg">${getPriorityStars(expense.priority)}</span>
                                </td>
                                <td class="py-4 px-6 text-center">
                                    ${!expense.paid && isRecommended
                ? '<span class="bg-blue-500 text-white px-3 py-1 rounded-full text-sm font-semibold">✓ Yes</span>'
                : !expense.paid
                    ? '<span class="text-gray-400 text-sm">Not in budget</span>'
                    : '<span class="text-gray-400 text-sm">—</span>'
            }
                                </td>
                                <td class="py-4 px-6 text-center">
                                    <span class="px-3 py-1 rounded-full text-sm font-semibold ${expense.paid
                ? 'bg-green-100 text-green-600'
                : 'bg-red-100 text-red-600'
            }">
                                        ${expense.paid ? 'Paid' : 'Unpaid'}
                                    </span>
                                </td>
                                <td class="py-4 px-6 text-center">
                                    <div class="flex gap-1 justify-center flex-wrap">
                                        ${!expense.paid
                ? `<button onclick="editExpense(${expense.id})"
                                               class="bg-blue-500 text-white px-2 py-1 rounded text-sm font-semibold hover:bg-blue-600">
                                               Edit
                                           </button>
                                           <button onclick="markExpensePaid(${expense.id})"
                                               class="bg-green-500 text-white px-2 py-1 rounded text-sm font-semibold hover:bg-green-600">
                                               Mark Paid
                                           </button>`
                : ''
            }
                                        <button onclick="deleteExpense(${expense.id})"
                                            class="bg-red-500 text-white px-2 py-1 rounded text-sm font-semibold hover:bg-red-600">
                                            Delete
                                        </button>
                                    </div>
                                </td>
                            </tr>
                        `;
    }).join('')}
                </tbody>
            </table>
        </div>
    `;

    // Show/update pagination if more than one page
    const totalPages = Math.ceil(filteredExpenses.length / pageSize);
    if (totalPages > 1) {
        showPagination(currentPage, totalPages);
    } else {
        hidePagination();
    }

    // Render recommendation card
    renderRecommendationCard(recommendedIds, availableBudget);
}

// ==================== NOTIFICATION SYSTEM ====================

function showNotification(message, type = 'success') {
    // Create notification element
    const notification = document.createElement('div');
    notification.className = `fixed top-4 right-4 z-50 px-6 py-4 rounded-lg shadow-lg transform transition-all duration-300 ${type === 'success' ? 'bg-green-500' : 'bg-red-500'
        } text-white font-semibold`;
    notification.textContent = message;
    notification.style.opacity = '0';
    notification.style.transform = 'translateY(-20px)';

    document.body.appendChild(notification);

    // Fade in
    setTimeout(() => {
        notification.style.opacity = '1';
        notification.style.transform = 'translateY(0)';
    }, 10);

    // Fade out and remove
    setTimeout(() => {
        notification.style.opacity = '0';
        notification.style.transform = 'translateY(-20px)';
        setTimeout(() => {
            document.body.removeChild(notification);
        }, 300);
    }, 3000);
}

// ==================== UTILITY FUNCTIONS ====================

function formatCurrency(amount) {
    return `₱${parseFloat(amount).toFixed(2)}`;
}

function formatDate(dateString) {
    const options = { year: 'numeric', month: 'short', day: 'numeric' };
    return new Date(dateString).toLocaleDateString('en-PH', options);
}

// ==================== EVENT SYNC (CROSS-PAGE & IN-APP) ====================

// When other code in same window dispatches 'kitako-data-changed', refresh UI
window.addEventListener('kitako-data-changed', function () {
    // Refresh in-memory first
    refreshDataFromStorage();

    if (typeof updateDashboard === 'function') updateDashboard();
    if (typeof updateUtangLogs === 'function') updateUtangLogs();
    if (typeof updateSalesTracker === 'function') updateSalesTracker();
    if (typeof updateExpensesPage === 'function') updateExpensesPage();
});

// When localStorage changes in other tabs/windows, update UI here
window.addEventListener('storage', function (e) {
    if (e.key === 'kitako_utangs' || e.key === 'kitako_sales' || e.key === 'kitako_daily_goal' || e.key === 'kitako_expenses' || e.key === 'kitako_budget') {
        refreshDataFromStorage();

        if (typeof updateDashboard === 'function') updateDashboard();
        if (typeof updateUtangLogs === 'function') updateUtangLogs();
        if (typeof updateSalesTracker === 'function') updateSalesTracker();
        if (typeof updateExpensesPage === 'function') updateExpensesPage();
    }
});

// ==================== KEYBOARD SHORTCUTS ====================

document.addEventListener('keydown', function (e) {
    // ESC to close modals
    if (e.key === 'Escape') {
        const saleModal = document.getElementById('saleModal');
        const utangModal = document.getElementById('utangModal');
        const goalModal = document.getElementById('goalModal');

        if (saleModal && !saleModal.classList.contains('hidden')) {
            closeSaleModal();
        }
        if (utangModal && !utangModal.classList.contains('hidden')) {
            closeUtangModal();
        }
        if (goalModal && !goalModal.classList.contains('hidden')) {
            closeGoalModal();
        }
    }

    // Ctrl/Cmd + S to open sale modal (if on dashboard/sales page)
    if ((e.ctrlKey || e.metaKey) && e.key === 's') {
        e.preventDefault();
        const saleModal = document.getElementById('saleModal');
        if (saleModal && saleModal.classList.contains('hidden')) {
            openSaleModal();
        }
    }
});

// ==================== EXPORT FUNCTIONS ====================

function exportToCSV(data, filename) {
    const csvContent = convertToCSV(data);
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);

    link.setAttribute('href', url);
    link.setAttribute('download', filename);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

function convertToCSV(data) {
    if (!data || data.length === 0) return '';

    const headers = Object.keys(data[0]);
    const csvRows = [headers.join(',')];

    for (const row of data) {
        const values = headers.map(header => {
            const value = row[header];
            return `"${value}"`;
        });
        csvRows.push(values.join(','));
    }

    return csvRows.join('\n');
}

// ==================== PRINT FUNCTIONS ====================

function printReport(title, content) {
    const printWindow = window.open('', '', 'height=600,width=800');
    printWindow.document.write('<html><head><title>' + title + '</title>');
    printWindow.document.write('<style>');
    printWindow.document.write('body { font-family: Arial, sans-serif; padding: 20px; }');
    printWindow.document.write('table { width: 100%; border-collapse: collapse; margin-top: 20px; }');
    printWindow.document.write('th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }');
    printWindow.document.write('th { background-color: #f97316; color: white; }');
    printWindow.document.write('h1 { color: #f97316; }');
    printWindow.document.write('</style>');
    printWindow.document.write('</head><body>');
    printWindow.document.write('<h1>' + title + '</h1>');
    printWindow.document.write(content);
    printWindow.document.write('</body></html>');
    printWindow.document.close();
    printWindow.print();
}

// ==================== DATA BACKUP & RESTORE ====================

function backupData() {
    const data = {
        sales: sales,
        utangs: utangs,
        exportDate: new Date().toISOString()
    };

    const dataStr = JSON.stringify(data, null, 2);
    const blob = new Blob([dataStr], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');

    link.setAttribute('href', url);
    link.setAttribute('download', `kitako-backup-${new Date().toISOString().split('T')[0]}.json`);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    showNotification('Data backed up successfully!', 'success');
}

function restoreData(fileInput) {
    const file = fileInput.files[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = function (e) {
        try {
            const data = JSON.parse(e.target.result);

            if (data.sales) {
                sales = data.sales;
                saveSales();
            }
            if (data.utangs) {
                utangs = data.utangs;
                saveUtangs();
            }

            // Refresh current page
            location.reload();

            showNotification('Data restored successfully!', 'success');
        } catch (error) {
            showNotification('Error restoring data. Invalid file format.', 'error');
        }
    };
    reader.readAsText(file);
}

// ==================== HELPER FUNCTIONS FOR UTANG AGING ====================

// Helper function: Get current amount with aging for a utang
function getCurrentAmount(utang) {
    const dueDate = new Date(utang.dueDate);
    const today = new Date();

    // If not yet due, return original amount
    if (dueDate >= today) {
        return utang.amount;
    }

    // Calculate penalty based on how many complete months overdue
    const penalty = getPenaltyAmount(utang);
    return utang.amount + penalty;
}

// Helper function: Calculate penalty amount
function getPenaltyAmount(utang) {
    const dueDate = new Date(utang.dueDate);
    const today = new Date();

    if (dueDate >= today) {
        return 0; // Not yet due, no penalty
    }

    // Calculate full months overdue
    let monthsOverdue = 0;
    let current = new Date(dueDate);
    while (current < today) {
        current.setMonth(current.getMonth() + 1);
        if (current <= today) {
            monthsOverdue++;
        }
    }

    // 5% penalty per full month overdue
    const penaltyRate = 0.05;
    return utang.amount * penaltyRate * monthsOverdue;
}

// Helper function: Get months overdue
function getMonthsOverdue(dueDate) {
    const due = new Date(dueDate);
    const today = new Date();

    if (due >= today) {
        return 0;
    }

    let months = 0;
    let current = new Date(due);
    while (current < today) {
        current.setMonth(current.getMonth() + 1);
        if (current <= today) {
            months++;
        }
    }

    return months;
}

// Notification system
function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `fixed top-4 right-4 px-6 py-3 rounded-lg text-white font-semibold ${
        type === 'success' ? 'bg-green-500' : 
        type === 'error' ? 'bg-red-500' : 
        'bg-blue-500'
    } shadow-lg z-50`;
    notification.textContent = message;
    
    document.body.appendChild(notification);
    
    setTimeout(() => {
        notification.remove();
    }, 3000);
}

// ==================== INITIALIZE ON PAGE LOAD ====================

document.addEventListener('DOMContentLoaded', function () {
    console.log('KitaKo initialized');
    console.log('Total Sales:', sales.length);
    console.log('Total Utangs:', utangs.length);
    // Ensure UI reflects stored data on load
    if (typeof updateDashboard === 'function') updateDashboard();
    if (typeof updateUtangLogs === 'function') updateUtangLogs();
    if (typeof updateSalesTracker === 'function') updateSalesTracker();
    if (typeof updateExpensesPage === 'function') updateExpensesPage();
});

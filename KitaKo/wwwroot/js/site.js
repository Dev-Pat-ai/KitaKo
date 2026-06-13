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

    if (!response.ok) {
        throw new Error(`Request failed: ${response.status}`);
    }

    if (response.status === 204) {
        return null;
    }

    return response.json();
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

    if (!amount || isNaN(amount) || (!Number.isFinite(profit) && profit !== 0) || isNaN(profit)) {
        alert('Please fill in amount and profit fields');
        return;
    }

    const sale = {
        amount: amount,
        profit: profit,
        description: description || 'Sale'
    };

    try {
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

// Cached result from the last server-side optimization call
let lastOptimizationResult = null;

/**
 * Call the backend /api/expenses/optimize endpoint.
 * Returns the full ExpenseOptimizationResult object (or null on error).
 */
async function runKnapsackOptimization(budget) {
    try {
        const result = await fetchJson('/api/expenses/optimize', {
            method: 'POST',
            body: JSON.stringify({ budget: parseFloat(budget) })
        });
        lastOptimizationResult = result;
        return result;
    } catch (err) {
        console.error('Knapsack optimization failed:', err);
        return null;
    }
}

/**
 * Quick client-side fallback knapsack used only for the expenses table highlight
 * when no server result is available yet.  Works in centavos (×100) to
 * preserve decimal precision — fixes the original floor() truncation bug.
 */
function knapsackOptimizeLocal(budget) {
    const unpaid = expenses.filter(e => !e.paid);
    const n = unpaid.length;
    // FIX: work in centavos
    const W = Math.round(budget * 100);
    if (n === 0 || W <= 0) return [];

    const now = Date.now();

    function computeValue(exp) {
        const daysLeft = (new Date(exp.dueDate) - now) / 86400000;
        const urgency = daysLeft < 0 ? 500
            : daysLeft <= 1 ? 400
                : daysLeft <= 3 ? 300
                    : daysLeft <= 7 ? 150
                        : daysLeft <= 14 ? 50 : 0;
        const catBonus = { bill: 200, subscription: 150, stock: 100, other: 50 }[exp.category] || 0;
        return exp.priority * 1000 + urgency + catBonus;
    }

    // 1-D rolling DP (space-efficient)
    const dp = new Int32Array(W + 1);
    for (let i = 0; i < n; i++) {
        const wi = Math.round(unpaid[i].amount * 100);
        const vi = computeValue(unpaid[i]);
        for (let w = W; w >= wi; w--) {
            if (dp[w - wi] + vi > dp[w]) dp[w] = dp[w - wi] + vi;
        }
    }

    // Backtrack with 2-D table
    const dpFull = Array.from({ length: n + 1 }, () => new Int32Array(W + 1));
    for (let i = 1; i <= n; i++) {
        const wi = Math.round(unpaid[i - 1].amount * 100);
        const vi = computeValue(unpaid[i - 1]);
        for (let w = 0; w <= W; w++) {
            dpFull[i][w] = dpFull[i - 1][w];
            if (wi <= w && dpFull[i - 1][w - wi] + vi > dpFull[i][w])
                dpFull[i][w] = dpFull[i - 1][w - wi] + vi;
        }
    }

    const selected = [];
    let rem = W;
    for (let i = n; i > 0 && rem > 0; i--) {
        if (dpFull[i][rem] !== dpFull[i - 1][rem]) {
            selected.push(unpaid[i - 1].id);
            rem -= Math.round(unpaid[i - 1].amount * 100);
        }
    }
    return selected;
}

// ==================== UI HELPERS ====================

function getPriorityStars(priority) {
    // Support 1-10 scale: show filled stars out of 10, clamped
    const p = Math.min(Math.max(parseInt(priority) || 1, 1), 10);
    return '★'.repeat(p) + '☆'.repeat(10 - p);
}

function getDaysUntilDue(dueDate) {
    const today = new Date();
    const due = new Date(dueDate);
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
    const modal = document.getElementById('expenseModal');
    if (!modal) return;
    modal.classList.remove('hidden');
    const nameEl = document.getElementById('expenseName');
    if (nameEl) nameEl.focus();
}

function closeExpenseModal() {
    const modal = document.getElementById('expenseModal');
    if (!modal) return;
    modal.classList.add('hidden');

    const nameEl = document.getElementById('expenseName');
    const amountEl = document.getElementById('expenseAmount');
    const dueEl = document.getElementById('expenseDueDate');
    const prEl = document.getElementById('expensePriority');
    const categoryEl = document.getElementById('expenseCategory');

    if (nameEl) nameEl.value = '';
    if (amountEl) amountEl.value = '';
    if (dueEl) dueEl.value = '';
    if (prEl) prEl.value = '5';
    if (categoryEl) categoryEl.value = 'bill';

    updatePriorityDisplay(5);
}

function updatePriorityDisplay(priority) {
    const stars = getPriorityStars(parseInt(priority));
    const disp = document.getElementById('priorityDisplay');
    if (disp) disp.textContent = `${stars} (${priority}/10)`;
}

// ==================== EXPENSE ACTIONS ====================

async function addExpense() {
    const nameEl = document.getElementById('expenseName');
    // FIX: ExpensePriority.cshtml uses id="expenseCost"; fall back to expenseAmount
    const amountEl = document.getElementById('expenseCost') || document.getElementById('expenseAmount');
    const dueDateEl = document.getElementById('expenseDueDate');
    const prEl = document.getElementById('expensePriority');
    const categoryEl = document.getElementById('expenseCategory');

    const name = nameEl ? nameEl.value.trim() : '';
    const amount = amountEl ? parseFloat(amountEl.value) : NaN;
    const dueDate = dueDateEl ? dueDateEl.value : '';
    const priority = prEl ? parseInt(prEl.value) : 5;
    const category = categoryEl ? categoryEl.value : 'other';

    if (!name || isNaN(amount) || amount <= 0 || !dueDate) {
        alert('Please fill in all required fields (name, amount, due date).');
        return;
    }

    const expense = { name, amount, dueDate, priority, category, paid: false };

    try {
        const createdExpense = await fetchJson('/api/expenses', {
            method: 'POST',
            body: JSON.stringify(expense)
        });
        expenses.push(createdExpense);
        closeExpenseModal();
        updateExpensesPage();
        showNotification('Expense added successfully!', 'success');
    } catch (error) {
        console.error('Error adding expense:', error);
        alert('Unable to add expense. Please try again.');
    }
}

async function markExpensePaid(id) {
    // Ensure latest state
    await Promise.all([refreshExpensesFromServer(), refreshFinancialSettingsFromServer()]);

    const idx = expenses.findIndex(e => e.id === id);
    if (idx === -1) return;

    const expense = expenses[idx];

    // If already paid, do nothing (avoid double-subtracting)
    if (expense.paid) {
        showNotification('Expense already marked as paid', 'success');
        return;
    }

    // Subtract expense amount from budget and persist
    const amt = parseFloat(expense.amount) || 0;
    availableBudget = parseFloat(availableBudget) || 0;
    // Prevent negative budget (clamp to 0). Remove Math.max(...) if you want negatives allowed.
    availableBudget = Math.max(0, availableBudget - amt);
    // Mark expense as paid and persist
    expenses[idx] = { ...expense, paid: true };

    try {
        await fetchJson(`/api/expenses/${id}`, {
            method: 'PUT',
            body: JSON.stringify(expenses[idx])
        });
        await saveFinancialSettings();
    } catch (error) {
        console.error('Error marking expense paid:', error);
        alert('Unable to mark expense as paid. Please try again.');
        return;
    }

    // Refresh UI
    updateExpensesPage();
    showNotification(`Expense marked as paid. Budget reduced by ₱${amt.toFixed(2)}.`, 'success');
}

async function deleteExpense(id) {
    if (!confirm('Delete this expense?')) return;

    try {
        await fetchJson(`/api/expenses/${id}`, { method: 'DELETE' });
    } catch (error) {
        console.error('Error deleting expense:', error);
        alert('Unable to delete expense. Please try again.');
        return;
    }

    expenses = expenses.filter(expense => expense.id !== id);
    updateExpensesPage();
    showNotification('Expense deleted!', 'success');
}

async function updateBudget() {
    const newBudget = prompt('Enter new available budget:', availableBudget);

    if (newBudget && !isNaN(newBudget)) {
        availableBudget = parseFloat(newBudget);
        await saveFinancialSettings();   // ← wait for server to confirm the new value
        await updateExpensesPage();      // ← now refresh; server has the correct value
        showNotification('Budget updated!', 'success');
    }
}

// ==================== PAGE RENDERING ====================

async function updateExpensesPage() {
    try {
        await Promise.all([refreshExpensesFromServer(), refreshFinancialSettingsFromServer()]);
    } catch (error) {
        console.error('Error refreshing expenses:', error);
    }

    // ── Compute stats ────────────────────────────────────────────────────────
    const totalUnpaid = expenses.filter(e => !e.paid).reduce((s, e) => s + e.amount, 0);
    const totalPaid = expenses.filter(e => e.paid).reduce((s, e) => s + e.amount, 0);
    const unpaidCount = expenses.filter(e => !e.paid).length;

    // ── Summary cards (ExpensePriority page) ─────────────────────────────────
    const pendingCountEl = document.getElementById('pendingExpensesCount');
    const totalPendingEl = document.getElementById('totalPendingExpenses');
    const totalPaidEl = document.getElementById('totalPaidExpenses');
    if (pendingCountEl) pendingCountEl.textContent = unpaidCount;
    if (totalPendingEl) totalPendingEl.textContent = `₱${totalUnpaid.toFixed(2)}`;
    if (totalPaidEl) totalPaidEl.textContent = `₱${totalPaid.toFixed(2)}`;

    // ── Summary cards (Expenses page) ────────────────────────────────────────
    const totalUnpaidEl = document.getElementById('totalUnpaid');
    const unpaidCountEl = document.getElementById('unpaidCount');
    const paidCountEl = document.getElementById('paidCount');
    if (totalUnpaidEl) totalUnpaidEl.textContent = `₱${totalUnpaid.toFixed(2)}`;
    if (unpaidCountEl) unpaidCountEl.textContent = unpaidCount;
    if (paidCountEl) paidCountEl.textContent = expenses.filter(e => e.paid).length;

    // ── Budget display ────────────────────────────────────────────────────────
    const availableBudgetEl = document.getElementById('availableBudget');

    // Always sync the input/display to the freshly-loaded server value.
    // (The What-If slider is separate and uses its own live value.)
    if (availableBudgetEl && availableBudgetEl.tagName === 'INPUT') {
        // Only overwrite if the user is NOT actively typing (input not focused)
        if (document.activeElement !== availableBudgetEl) {
            availableBudgetEl.value = availableBudget.toFixed(2);
        }
    } else if (availableBudgetEl) {
        availableBudgetEl.textContent = `₱${availableBudget.toFixed(2)}`;
    }

    // ── Overdue warning banner ────────────────────────────────────────────────
    const now = new Date();
    const overdueItems = expenses.filter(e => !e.paid && new Date(e.dueDate) < now);
    const overdueEl = document.getElementById('overdueWarningBanner');
    if (overdueEl) {
        if (overdueItems.length > 0) {
            const overdueTotal = overdueItems.reduce((s, e) => s + e.amount, 0);
            overdueEl.innerHTML = `
                <div class="flex items-center gap-3">
                    <span class="text-2xl">⚠️</span>
                    <div>
                        <strong>You have ${overdueItems.length} overdue expense(s)</strong> totalling
                        <strong>₱${overdueTotal.toFixed(2)}</strong>.
                        These are automatically given the highest priority in optimization.
                    </div>
                </div>`;
            overdueEl.classList.remove('hidden');
        } else {
            overdueEl.classList.add('hidden');
        }
    }

    // ── Budget coverage bar ───────────────────────────────────────────────────
    const coverageBarEl = document.getElementById('budgetCoverageBar');
    const coverageLabelEl = document.getElementById('budgetCoverageLabel');
    if (coverageBarEl && totalUnpaid > 0) {
        const pct = Math.min(100, (availableBudget / totalUnpaid) * 100);
        coverageBarEl.style.width = `${pct.toFixed(1)}%`;
        coverageBarEl.className = `h-full rounded-full transition-all duration-500 ${pct >= 100 ? 'bg-green-500' : pct >= 60 ? 'bg-yellow-400' : 'bg-red-500'
            }`;
        if (coverageLabelEl) {
            coverageLabelEl.textContent =
                `Budget covers ${pct.toFixed(0)}% of total obligations ` +
                `(₱${availableBudget.toFixed(2)} of ₱${totalUnpaid.toFixed(2)})`;
        }
    }

    // ── Use backend optimizer (server-side Knapsack), fall back to local ──────
    const optimizeBudget = availableBudgetEl && availableBudgetEl.tagName === 'INPUT'
        ? parseFloat(availableBudgetEl.value) || availableBudget
        : availableBudget;

    let optimizedIds = [];
    const result = await runKnapsackOptimization(optimizeBudget);

    if (result) {
        optimizedIds = (result.recommendedExpenses || []).map(e => e.id);
        renderOptimizationResult(result, optimizeBudget);
    } else {
        // Fallback to local algorithm
        optimizedIds = knapsackOptimizeLocal(optimizeBudget);
    }

    const optimizedTotal = expenses
        .filter(e => optimizedIds.includes(e.id))
        .reduce((s, e) => s + e.amount, 0);

    const optimizedCostEl = document.getElementById('optimizedCost');
    if (optimizedCostEl) optimizedCostEl.textContent = `₱${optimizedTotal.toFixed(2)}`;

    renderExpensesTable(optimizedIds);
}

/**
 * Render the optimization result panel including recommended, skipped, warning,
 * priority score, and remaining budget.
 */
function renderOptimizationResult(result, budget) {
    const container = document.getElementById('knapsackResults');
    const aiEl = document.getElementById('aiRecommendation');

    const recommended = result.recommendedExpenses || [];
    const skipped = result.skippedExpenses || [];
    const totalCost = result.totalOptimizedCost || 0;
    const remaining = result.remainingBudget || (budget - totalCost);
    const score = result.totalPriorityScore || 0;
    const warning = result.budgetWarning;

    const html = `
        ${warning ? `
        <div class="bg-red-50 border border-red-300 text-red-800 rounded-xl p-4 mb-4 flex items-start gap-3">
            <span class="text-xl mt-0.5">🔴</span>
            <p class="text-sm">${warning}</p>
        </div>` : ''}

        <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-6">
            <!-- Recommended -->
            <div class="bg-gradient-to-br from-blue-500 to-blue-600 rounded-xl p-6 shadow-lg text-white">
                <h4 class="text-lg font-bold mb-3 flex items-center gap-2">
                    ✅ Pay These First
                    <span class="text-sm font-normal opacity-80">(${recommended.length} items)</span>
                </h4>
                ${recommended.length === 0
            ? '<p class="text-blue-100 text-sm">Budget is too low to cover any expense.</p>'
            : `<ul class="space-y-2">
                        ${recommended.map(e => `
                        <li class="flex justify-between items-center bg-white/10 rounded-lg px-3 py-2">
                            <div>
                                <span class="font-semibold">${e.name}</span>
                                ${e.category ? `<span class="ml-2 text-xs opacity-70 uppercase">${e.category}</span>` : ''}
                            </div>
                            <span class="font-bold">₱${parseFloat(e.amount).toFixed(2)}</span>
                        </li>`).join('')}
                       </ul>
                       <div class="mt-4 pt-3 border-t border-white/20 flex justify-between text-sm">
                           <span>Total</span>
                           <span class="font-bold">₱${totalCost.toFixed(2)}</span>
                       </div>
                       <div class="flex justify-between text-blue-100 text-sm mt-1">
                           <span>Remaining budget</span>
                           <span>₱${remaining.toFixed(2)}</span>
                       </div>`
        }
                <div class="mt-3 text-xs text-blue-200">Priority Score: ${score.toLocaleString()}</div>
            </div>

            <!-- Skipped -->
            <div class="bg-white rounded-xl p-6 shadow-md border border-gray-200">
                <h4 class="text-lg font-bold mb-3 text-gray-700 flex items-center gap-2">
                    ⏸ Deferred
                    <span class="text-sm font-normal text-gray-400">(${skipped.length} items)</span>
                </h4>
                ${skipped.length === 0
            ? '<p class="text-gray-400 text-sm">All unpaid expenses fit within the budget! 🎉</p>'
            : `<ul class="space-y-2">
                        ${skipped.map(e => {
                const daysLeft = Math.ceil((new Date(e.dueDate) - new Date()) / 86400000);
                const urgency = daysLeft < 0 ? 'text-red-600'
                    : daysLeft <= 7 ? 'text-orange-500'
                        : 'text-gray-400';
                return `
                            <li class="flex justify-between items-center py-2 border-b border-gray-100 last:border-0">
                                <div>
                                    <span class="text-gray-700 font-medium">${e.name}</span>
                                    <span class="ml-2 text-xs ${urgency}">
                                        ${daysLeft < 0 ? `${Math.abs(daysLeft)}d overdue` : `${daysLeft}d left`}
                                    </span>
                                </div>
                                <span class="text-gray-500 font-semibold">₱${parseFloat(e.amount).toFixed(2)}</span>
                            </li>`;
            }).join('')}
                       </ul>`
        }
            </div>
        </div>
    `;

    if (container) container.innerHTML = html;

    // Also update the legacy aiRecommendation div if it exists (Expenses page)
    if (aiEl) {
        aiEl.innerHTML = `
        <div class="flex-1">
            <h3 class="text-2xl font-bold mb-2">Expense Recommendation</h3>
            <p class="text-blue-100 mb-4">Optimized for ₱${budget.toFixed(2)} budget — ${recommended.length} expense(s) selected.</p>
            <div class="bg-white/10 rounded-xl p-4">
                ${recommended.length === 0
                ? '<p class="text-blue-100">Budget too low for any expense.</p>'
                : `<ul class="space-y-2">${recommended.map(e =>
                    `<li class="flex justify-between"><span>✓ ${e.name}</span><span class="font-semibold">₱${parseFloat(e.amount).toFixed(2)}</span></li>`
                ).join('')}</ul>`
            }
                <div class="mt-4 pt-4 border-t border-white/20 flex justify-between font-semibold">
                    <span>Total</span><span>₱${totalCost.toFixed(2)}</span>
                </div>
                <div class="flex justify-between text-blue-100 text-sm mt-1">
                    <span>Remaining</span><span>₱${remaining.toFixed(2)}</span>
                </div>
            </div>
        </div>`;
    }
}

// ==================== UPDATE EXPENSES TABLE (now a function) ====================

function renderExpensesTable(optimizedIds = []) {
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
                        <th class="text-center py-4 px-6 text-gray-600 font-semibold">AI Recommends</th>
                        <th class="text-center py-4 px-6 text-gray-600 font-semibold">Status</th>
                        <th class="text-center py-4 px-6 text-gray-600 font-semibold">Actions</th>
                    </tr>
                </thead>
                <tbody>
                    ${[...expenses].sort((a, b) => {
        // Unpaid items first
        if (a.paid !== b.paid) {
            return a.paid ? 1 : -1; // unpaid (false) comes first
        }
        // If both are unpaid: higher priority first, then earlier due date
        if (!a.paid && !b.paid) {
            if (a.priority !== b.priority) return b.priority - a.priority; // desc priority
            return new Date(a.dueDate) - new Date(b.dueDate);
        }
        // If both are paid (or same status but not unpaid case), sort by due date
        return new Date(a.dueDate) - new Date(b.dueDate);
    }).map(expense => {
        const daysUntilDue = getDaysUntilDue(expense.dueDate);
        const isRecommended = optimizedIds.includes(expense.id);
        const urgencyClass = getUrgencyClass(daysUntilDue);

        // Row classes: unpaid rows get a light highlight; recommended stays blue
        const rowClasses = ['border-b', 'border-gray-100'];
        if (!expense.paid) rowClasses.push('bg-yellow-50');
        if (isRecommended && !expense.paid) rowClasses.push('bg-blue-50');

        return `
                            <tr class="${rowClasses.join(' ')}">
                                
                                <!-- Expense Name -->
                                <td class="py-4 px-6">
                                    <div class="flex items-center gap-2">
                                        ${expense.paid
                ? `<svg class="w-5 h-5 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                                       <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                                             d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z">
                                                       </path>
                                                   </svg>`
                : ''
            }
                                        <span class="font-semibold ${expense.paid ? 'text-gray-400 line-through' : 'text-gray-800'}">
                                            ${expense.name}
                                        </span>
                                    </div>
                                </td>

                                <!-- Amount -->
                                <td class="py-4 px-6 text-right font-semibold text-gray-800">
                                    ₱${expense.amount.toFixed(2)}
                                </td>

                                <!-- Due Date -->
                                <td class="py-4 px-6 text-center">
                                    <p class="text-gray-800">
                                        ${new Date(expense.dueDate).toLocaleDateString()}
                                    </p>
                                    ${!expense.paid
                ? `<p class="text-xs font-semibold ${urgencyClass} px-2 py-1 rounded inline-block mt-1">
                                                   ${daysUntilDue < 0
                    ? `${Math.abs(daysUntilDue)} days overdue`
                    : `${daysUntilDue} days left`}
                                               </p>`
                : ''
            }
                                </td>

                                <!-- Priority -->
                                <td class="py-4 px-6 text-center">
                                    <span class="text-yellow-500 text-lg">
                                        ${getPriorityStars(expense.priority)}
                                    </span>
                                </td>

                                <!-- AI Recommendation -->
                                <td class="py-4 px-6 text-center">
                                    ${!expense.paid && isRecommended
                ? '<span class="bg-blue-500 text-white px-3 py-1 rounded-full text-sm font-semibold">✓ Recommended</span>'
                : !expense.paid
                    ? '<span class="text-gray-400 text-sm">Not in budget</span>'
                    : '<span class="text-gray-400 text-sm">—</span>'
            }
                                </td>

                                <!-- Status -->
                                <td class="py-4 px-6 text-center">
                                    <span class="px-3 py-1 rounded-full text-sm font-semibold ${expense.paid
                ? 'bg-green-100 text-green-600'
                : 'bg-red-100 text-red-600'
            }">
                                        ${expense.paid ? 'Paid' : 'Unpaid'}
                                    </span>
                                </td>

                                <!-- Actions -->
                                <td class="py-4 px-6 text-center">
                                    <div class="flex gap-2 justify-center">
                                        ${!expense.paid
                ? `<button
                                                       onclick="markExpensePaid(${expense.id})"
                                                       class="bg-green-500 text-white px-3 py-1 rounded-lg text-sm font-semibold hover:bg-green-600 transition-colors">
                                                       Mark Paid
                                                   </button>`
                : ''
            }
                                        <button
                                            onclick="deleteExpense(${expense.id})"
                                            class="bg-red-500 text-white px-3 py-1 rounded-lg text-sm font-semibold hover:bg-red-600 transition-colors">
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

// Notification system
function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `fixed top-4 right-4 px-6 py-3 rounded-lg text-white font-semibold ${type === 'success' ? 'bg-green-500' :
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

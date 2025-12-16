// Site-wide JavaScript
console.log('BankApp Web geladen');

async function loadRecentTransactions(containerId = 'recentTransactionsContainer', count =5) {
    try {
        const res = await fetch(`/Transacties/RecentPartial?count=${count}`);
        if (!res.ok) throw new Error('Network response was not ok');
        const html = await res.text();
        const container = document.getElementById(containerId);
        if (container) container.innerHTML = html;
    } catch (err) {
        console.error('Fout bij laden recente transacties', err);
    }
}

// Auto-refresh example: refresh every 60 seconds
setInterval(() => loadRecentTransactions(), 60000);

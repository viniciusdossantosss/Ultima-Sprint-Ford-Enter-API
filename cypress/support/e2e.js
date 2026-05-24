import './commands';

// Ignorar exceções não tratadas disparadas pela aplicação (como CDNs externas do Bootstrap/FullCalendar)
Cypress.on('uncaught:exception', (err, runnable) => {
    return false;
});

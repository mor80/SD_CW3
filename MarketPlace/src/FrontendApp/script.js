const API_GATEWAY_URL = 'http://localhost:8080';
const WEBSOCKET_URL = 'ws://localhost:5000/ws';

// Инициализация приложения
document.addEventListener('DOMContentLoaded', () => {
    initTabs();
});

// Функция для инициализации вкладок
function initTabs() {
    const tabButtons = document.querySelectorAll('.tab-btn');
    
    tabButtons.forEach(button => {
        button.addEventListener('click', () => {
            const tabName = button.getAttribute('data-tab');
            
            // Деактивировать все вкладки
            document.querySelectorAll('.tab-btn').forEach(btn => {
                btn.classList.remove('active');
            });
            document.querySelectorAll('.tab-content').forEach(content => {
                content.classList.remove('active');
            });
            
            // Активировать выбранную вкладку
            button.classList.add('active');
            document.getElementById(`${tabName}-tab`).classList.add('active');
        });
    });
}

// Функция для вызова API
async function callApi(method, path, body = null, resultElementId) {
    const resultElement = document.getElementById(resultElementId);
    resultElement.textContent = 'Загрузка...';
    resultElement.className = 'result';

    try {
        const options = {
            method: method,
            headers: {
                'Content-Type': 'application/json',
            },
        };
        if (body) {
            options.body = JSON.stringify(body);
        }

        const response = await fetch(`${API_GATEWAY_URL}/${path}`, options);
        const data = await response.json();

        if (response.ok) {
            resultElement.textContent = JSON.stringify(data, null, 2);
            resultElement.classList.add('success');
            return data;
        } else {
            resultElement.textContent = `Ошибка: ${JSON.stringify(data, null, 2)}`;
            resultElement.classList.add('error');
            showNotification('Ошибка', data.message || 'Произошла ошибка при выполнении запроса', 'error');
            return null;
        }
    } catch (error) {
        resultElement.textContent = `Сетевая ошибка: ${error.message}`;
        resultElement.classList.add('error');
        showNotification('Ошибка соединения', error.message, 'error');
        return null;
    }
}

// Функция для отображения уведомлений
function showNotification(title, message, type = 'success') {
    const container = document.getElementById('notification-area');
    const notificationDiv = document.createElement('div');
    notificationDiv.className = `notification ${type}`;
    
    // Добавляем иконку в зависимости от типа уведомления
    let icon = 'check-circle';
    if (type === 'error') icon = 'exclamation-circle';
    if (type === 'warning') icon = 'exclamation-triangle';
    if (type === 'info') icon = 'info-circle';
    
    notificationDiv.innerHTML = `
        <i class="fas fa-${icon}"></i>
        <div>
            <strong>${title}</strong>
            <p>${message}</p>
        </div>
    `;

    container.appendChild(notificationDiv);

    // Удаление уведомления через 5 секунд
    setTimeout(() => {
        notificationDiv.classList.add('hide');
        notificationDiv.addEventListener('animationend', () => {
            notificationDiv.remove();
        });
    }, 5000);
}

// Функция для подключения к WebSocket
function connectWebSocket(orderId) {
    const ws = new WebSocket(WEBSOCKET_URL);

    ws.onopen = () => {
        console.log(`WebSocket подключен для заказа ${orderId}`);
        ws.send(orderId);
        showNotification('Подключено', `Отслеживание статуса заказа ${orderId} активировано`, 'info');
    };

    ws.onmessage = (event) => {
        const data = JSON.parse(event.data);
        console.log('Получено сообщение WebSocket:', data);
        if (data.orderId === orderId) {
            showNotification('Обновление заказа', `Статус заказа ${data.orderId} изменен на: ${data.status}`);
            if (data.status === 'Finished' || data.status === 'Cancelled') {
                ws.close();
            }
        }
    };

    ws.onclose = (event) => {
        console.log('WebSocket отключен:', event);
    };

    ws.onerror = (error) => {
        console.error('Ошибка WebSocket:', error);
        showNotification('Ошибка WebSocket', 'Подробности в консоли', 'error');
    };
}

// Функции для работы со счетами
async function createAccount() {
    const userId = document.getElementById('createAccountUserId').value;
    if (!userId) {
        showNotification('Ошибка валидации', 'Пожалуйста, введите ID пользователя', 'warning');
        return;
    }
    const result = await callApi('POST', 'accounts', { userId }, 'createAccountResult');
    if (result) {
        showNotification('Успех', `Счет для пользователя ${userId} успешно создан`);
    }
}

async function deposit() {
    const userId = document.getElementById('depositUserId').value;
    const amount = parseFloat(document.getElementById('depositAmount').value);
    if (!userId || isNaN(amount)) {
        showNotification('Ошибка валидации', 'Пожалуйста, введите корректные данные', 'warning');
        return;
    }
    const result = await callApi('POST', 'accounts/deposit', { userId, amount }, 'depositResult');
    if (result) {
        showNotification('Успех', `На счет пользователя ${userId} зачислено ${amount}`);
    }
}

async function getBalance() {
    const userId = document.getElementById('balanceUserId').value;
    if (!userId) {
        showNotification('Ошибка валидации', 'Пожалуйста, введите ID пользователя', 'warning');
        return;
    }
    const result = await callApi('GET', `accounts/balance?userId=${userId}`, null, 'balanceResult');
    if (result) {
        showNotification('Информация', `Баланс пользователя ${userId} успешно получен`, 'info');
    }
}

// Функции для работы с заказами
async function createOrder() {
    const userId = document.getElementById('createOrderUserId').value;
    const amount = parseFloat(document.getElementById('createOrderAmount').value);
    const description = document.getElementById('createOrderDescription').value;

    if (!userId || isNaN(amount) || !description) {
        showNotification('Ошибка валидации', 'Пожалуйста, заполните все поля', 'warning');
        return;
    }
    const orderData = await callApi('POST', 'orders', { userId, amount, description }, 'createOrderResult');
    if (orderData && orderData.id) {
        showNotification('Успех', `Заказ ${orderData.id} успешно создан`);
        connectWebSocket(orderData.id);
    }
}

async function getOrders() {
    const userId = document.getElementById('getOrdersUserId').value;
    if (!userId) {
        showNotification('Ошибка валидации', 'Пожалуйста, введите ID пользователя', 'warning');
        return;
    }
    const result = await callApi('GET', `orders?userId=${userId}`, null, 'getOrdersResult');
    if (result) {
        const count = Array.isArray(result) ? result.length : 0;
        showNotification('Информация', `Найдено ${count} заказов пользователя ${userId}`, 'info');
    }
}

async function getOrderStatus() {
    const orderId = document.getElementById('getOrderStatusId').value;
    if (!orderId) {
        showNotification('Ошибка валидации', 'Пожалуйста, введите ID заказа', 'warning');
        return;
    }
    const result = await callApi('GET', `orders/${orderId}/status`, null, 'getOrderStatusResult');
    if (result) {
        showNotification('Информация', `Статус заказа ${orderId} успешно получен`, 'info');
    }
} 
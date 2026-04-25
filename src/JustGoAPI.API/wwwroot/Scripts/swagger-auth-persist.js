(function () {
    const TOKEN_KEY = "swagger-jwt-token";
    function saveToken(token) {
        localStorage.setItem(TOKEN_KEY, token);
    }

    function getToken() {
        return localStorage.getItem(TOKEN_KEY);
    }

    function applyToken(token) {
        if (window.ui) {
            window.ui.preauthorizeApiKey("Bearer", token);
        }
    }

    // Wait for Swagger UI to load
    const waitForSwaggerUI = setInterval(() => {
        const token = getToken();
        if (window.ui && token) {
            applyToken(token);
            clearInterval(waitForSwaggerUI);
        }
    }, 500);

    // Intercept and persist token when authorized
    const waitForAuthAction = setInterval(() => {
        if (window.ui?.authActions?.authorize) {
            const originalAuthorize = window.ui.authActions.authorize;
            window.ui.authActions.authorize = function (payload) {
                const token = payload?.Bearer?.value;
                if (token) {
                    saveToken(token);
                }
                return originalAuthorize.call(this, payload);
            };
            clearInterval(waitForAuthAction);
        }
    }, 500);
})();


// reinit-swagger-with-hierarchy.js

(function () {
    const retryInterval = 100; // ms
    const maxRetries = 50;
    let retries = 0;

    const applyPlugin = function () {
        if (window.ui && window.SwaggerUIBundle && window.HierarchicalTagsPlugin) {
            const config = window.ui.getConfigs ? window.ui.getConfigs() : {
                dom_id: "#swagger-ui"
            };

            window.ui = SwaggerUIBundle({
                ...config,
                plugins: [...(config.plugins || []), window.HierarchicalTagsPlugin()],
                dom_id: config.dom_id || "#swagger-ui",
                hierarchicalTagSeparator: /[:|/]/,
            });

            console.info("Hierarchy plugin injected into Swagger UI.");
            return true;
        }

        return false;
    };

    const interval = setInterval(() => {
        if (++retries > maxRetries) {
            clearInterval(interval);
            console.warn("Hierarchy plugin injection timed out.");
            return;
        }

        if (applyPlugin()) {
            clearInterval(interval);
        }
    }, retryInterval);
})();

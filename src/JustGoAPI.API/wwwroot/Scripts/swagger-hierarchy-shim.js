// swagger-hierarchy-shim.js

(function () {
    // Try to find the plugin from known global exports
    const plugin = window.swaggerhierarchyplugin || window.default || window.HierarchicalTagsPlugin || null;

    if (plugin) {
        window.HierarchicalTagsPlugin = plugin;
    } else {
        console.warn("Hierarchy plugin not found. Check plugin export in swagger-hierarchy-plugin.js");
    }
})();

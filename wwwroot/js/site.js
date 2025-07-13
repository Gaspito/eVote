

/**
 * Simple awaitable timer
 * @param {Number} seconds time to wait for
 */
function wait(seconds) {
    return new Promise(resolve => {
        setTimeout(resolve, seconds * 1000);
    });
}
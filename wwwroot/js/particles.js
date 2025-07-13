

/**
 * Simple implementation of particles visual effects.
 * This function spawns a number of particles at the given coordinates.
 * The particles can have custom content (text/emojis)
 * @param {Number} x X coordinate to spawn the particles at
 * @param {Number} y Y coordinate to spawn the particles at
 * @param {String} content The text the particles will show
 * @param {Number} lifetime How long the particles will last, in seconds
 * @param {Number} count How many particles to spawn
 * @returns 
 */
async function spawnParticles(x, y, content="+", lifetime=3, count=1) {
    let particleContainer = document.getElementById("particles");
    if (!particleContainer) {
        particleContainer = document.createElement("div");
        particleContainer.style.display = "box";
        particleContainer.style.position = "fixed";
        particleContainer.style.pointerEvents = "none";
        particleContainer.style.left = "0";
        particleContainer.style.top = "0";
        particleContainer.style.right = "0";
        particleContainer.style.bottom = "0";
        particleContainer.style.zIndex = "500";
        // particleContainer.style.overflow = "hidden";
        document.body.append(particleContainer);
    }

    if (count <= 0) return;

    for (let i = 0; i < count; i++) {
        let particle = document.createElement("span");
        particle.innerText = content;
        particle.style.display = "box";
        particle.style.position = "absolute";
        particle.style.pointerEvents = "none";
        particle.style.left = `${x}px`;
        particle.style.top = `${y}px`;
        particle.style.transformOrigin = "center";
        particle.style.translate = "0 0";
        particle.style.scale = "0%";
        // particle.style.rotate = `${Math.random() * 365}deg`;
        particle.style.opacity = "100%";
        particle.style.transition = `scale 150ms ease-in-out, translate ${lifetime}s ease-out, opacity ${lifetime - Math.random() * lifetime * 0.5}s ease-out`;
        particleContainer.append(particle);
        setTimeout(() => {
            let angle = Math.random() * Math.PI * 2;
            particle.style.translate = `${Math.cos(angle) * 300}px ${Math.sin(angle) * 300}px`;
            particle.style.opacity = "0%";
            particle.style.scale = "100%";
            setTimeout(() => {
                particle.remove();
            }, lifetime * 1000);
        }, 10);
        await wait(0.05);
    }
}
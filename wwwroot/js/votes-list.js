/*
    This script defines functions for interacting with the votes of the user,
    such as listing the votes, casting new ones, etc.
*/

/**
 * Refreshes the vote list contained by the element found with targetQuery.
 * @param {String} targetQuery 
 * @param {object} options 
 * button: optional button to disable during the process,
 * remaining: element to write the remaining vote count into,
 * total: element to write the total vote count available into
 * @returns 
 */
async function refreshVotesList(targetQuery, options={}) {
    // If any, disable the button to avoid concurrency 
    // and show the user it's working
    options.button?.toggleAttribute("disabled", true);

    const target = document.querySelector(targetQuery);
    if (!target) return;

    // Wait a couple seconds for user trust
    if (options.button)
        await wait(2);

    const resp = await fetch(`/Vote/Votes`);
    const json = await resp.json();

    // Update the interface after the request
    options.button?.toggleAttribute("disabled", false);
    window.userVotes = json.votes.map(i => i.candidateEmail);
    target.innerHTML = json.votes.map( i => 
        `<li class="candidate no-vote-count" style="--progress: 0%;">
            <span class="candidate-name" title="${i.candidateEmail}">${i.candidateEmail}</span>
            <span></span>
            <button class="btn btn-danger" onclick="removeVote('${i.candidateEmail}', {button: this})">Remove</button>
        </li>`
    ).join("\n");
    if (options.remaining) {
        let element = document.querySelector(options.remaining);
        if (element) element.innerText = json.remainingVoteCount;
    }
    if (options.total) {
        let element = document.querySelector(options.total);
        if (element) element.innerText = json.maxVoteCount;
    }

    // TO DO: either do server side rendering of the list, or use templates for easier layout
}


/**
 * Sends a cast vote request to the server
 * @param {String} candidateEmail Email of the candidate to vote for
 * @param {object} options 
 * button: element to disable during the process
 * @returns 
 */
async function addVote(candidateEmail, options={}) {
    options.button?.toggleAttribute("disabled", true);
    let now = performance.now();
    const resp = await fetch(`/Vote/AddVote`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({
            Email: candidateEmail,
        }),
    });
    if (options.button) {
        const bbox = options.button.getBoundingClientRect();
        spawnParticles(bbox.x+bbox.width*0.5, bbox.y+bbox.height*0.5, "✔️", 5, 10);
        options.button.style.setProperty("scale", "0%");
        // TO DO: make fx a response to an event instead of hard coded
    }
    // Wait 3 seconds so the user has more trust that the app is doing something
    now = performance.now() -now;
    if (now < 3000) {
        await wait(now * 0.001);
    }
    if (!resp.ok) {
        options.button?.toggleAttribute("disabled", false);
        console.error(resp.status, resp.statusText);
        await createErrorModal("Error", `There was a problem casting your vote: \n ${await resp.text()}`);
        return;
    }
    options.button?.style.setProperty("opacity", "0%");
}


/**
 * Sends a remove vote request to the server
 * @param {String} candidateEmail Email of the candidate
 * @param {object} options 
 * button: element to disable during the request
 * @returns 
 */
async function removeVote(candidateEmail, options={}) {
    options.button?.toggleAttribute("disabled", true);
    let now = performance.now();
    const resp = await fetch(`/Vote/RemoveVote`, {
        method: "DELETE",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({
            Email: candidateEmail,
        }),
    });
    if (options.button) {
        const bbox = options.button.getBoundingClientRect();
        spawnParticles(bbox.x+bbox.width*0.5, bbox.y+bbox.height*0.5, "❌", 5, 10);
        options.button.style.setProperty("scale", "0%");
        // TO DO: make fx a response to an event instead of hard coded
    }
    // Wait 3 seconds so the user has more trust that the app is doing something
    now = performance.now() -now;
    if (now < 3000) {
        await wait(now * 0.001);
    }
    if (!resp.ok) {
        options.button?.toggleAttribute("disabled", false);
        console.error(resp.status, resp.statusText);
        await createErrorModal("Error", `There was a problem removing your vote: \n ${await resp.text()}`);
        return;
    }
    options.button?.style.setProperty("opacity", "0%");
}

/**
 * Refreshes the remaining vote count on the interface
 * @param {String} targetQuery query that returns the element to write the result into
 */
async function refreshRemainingVotes(targetQuery) {
    const target = document.querySelector(targetQuery);
    if (!target) return;
    const resp = await fetch(`/Vote/Remaining`);
    // Do not handle !resp.ok condition, so that errors are logged
    const json = await resp.json();
    target.innerText = `${json.remainingVotes}`;
}

/**
 * Refreshes the current vote count (when user is a Candidate) on the UI
 * @param {String} targetQuery query that identifies the element to update
 */
async function refreshCurrentVotes(targetQuery) {
    const target = document.querySelector(targetQuery);
    if (!target) return;
    const resp = await fetch(`/Vote/Count`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({Email: window.userEmail}),
    });
    // Do not handle !resp.ok to allow errors to log
    const json = await resp.json();
    target.innerText = `${json.votes}`;
}


/**
 * Sends a final vote count request to the server (for admin users)
 * The result is shown in a popup and can be printed.
 * TO DO: put this function in a separate script, that only admin users
 * can get (even though all validation happens serverside anyway)
 * @param {String} targetQuery deprecated
 * @param {Object} options 
 * button: the element to disable while the process runs
 */
async function requestFinalVoteCount(targetQuery="", options={}) {
    options.button?.toggleAttribute("disabled", true);
    const target = document.querySelector(targetQuery);
    if (options.button)
        await wait(2);
    const resp = await fetch(`/Vote/FinalCount`, {method: "POST"});
    const json = await resp.json();
    options.button?.toggleAttribute("disabled", false);
    const candidates = json.candidates;
    const remainingVoters = json.remainingVoters;

    // If not all votes are cast, issue a warning
    if (remainingVoters > 0) {
        if (! await createConfirmModal("Vote not complete", `${remainingVoters} voters have not cast all their votes yet. Are you sure you want to see the final vote count now?`))
        {
            return;
        }
    }

    const candidateListHtml = candidates.map( i => 
        `<tr>
            <td>${i.candidateEmail}</td>
            <td>${i.voteCount}</td>
        </tr>`
    ).join("\n");

    const popup = window.open("", null, "height=200,width=400,status=yes,toolbar=no,menubar=no,location=no");
    popup.document.body.innerHTML = `
        <h1>eVote - Final Vote Count</h1>
        <table>
            <tr>
                <th>Candidate Email</th>
                <th>Vote Count</th>
            </tr>
            ${candidateListHtml}
        </table>
        <style>
            :root {
                font-family: 'Helvetica', sans-serif;
            }
            table {
                border-collapse: collapse;
            }
            td, th {
                border: 1pt solid black;
                padding: 1rem;
            }
        </style>
    `;
}


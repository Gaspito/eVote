/*
    This script defines functions to interact with the candidate list
*/

/**
 * Gets the updated candidate list, ordered by vote count, and injects it
 * into the element identified by targetQuery
 * @param {String} targetQuery query that identifies the element to put the candidate list into
 * @param {Object} options
 * button: element to disable during the process
 * canVote: boolean that toggles the display of the 'Vote' buttons
 */
async function refreshCandidateList(targetQuery, options={}) {
    options.button?.toggleAttribute("disabled", true);
    const target = document.querySelector(targetQuery);
    // If a button is passed, wait a few seconds to build user trust
    if (options.button)
        await wait(2);
    const resp = await fetch("/Vote/Candidates");
    const json = await resp.json();
    options.button?.toggleAttribute("disabled", false);
    const totalVotes = json.totalVotes || 1;
    const candidates = json.candidates;
    const remainingVotes = json.remainingUserVotes;
    const canVote = window.userRole == "Voter" && options.canVote !== false;
    const userVotes = window.userVotes || [];
    target.innerHTML = candidates.map( i => 
        `<li class="candidate" style="--progress: ${i.voteCount / totalVotes * 100}%;">
        <span class="candidate-name" title="${i.email}">${i.email == window.userEmail ? "<strong>You</strong>" : i.email}</span> <span class="vote-count">${i.voteCount}</span>
        ${canVote ? 
            (userVotes.includes(i.email) || remainingVotes == 0 ? 
                `<button class="btn btn-secondary" disabled>Vote</button>`
                : `<button class="btn btn-primary" onclick="addVote('${i.email}', {button: this})">Vote</button>`
            )
            : "<span></span>"}
        </li>`
    ).join("\n");
    // TO DO: switch to complete server side rendering or use templates for easier layouts
}

/**
 * Sends a register as Candidate request to the server
 * If successful, the page will refresh
 */
async function registerAsCandidate() {
    const resp = await fetch("/Vote/RegisterAsCandidate", {method: "PUT"});
    if (resp.ok) {
        open("/Vote/Index", "_self");
        return;
    }
    console.error(resp.status, resp.statusText);
    await createErrorModal("Error", `There was a problem registering you as a candidate: \n ${await resp.text()}`);
}


/**
 * Sends a register as Voter request to the server
 * If successful, the page will refresh
 */
async function registerAsVoter() {
    const resp = await fetch("/Vote/RegisterAsVoter", {method: "PUT"});
    if (resp.ok) {
        open("/Vote/Index", "_self");
        return;
    }
    console.error(resp.status, resp.statusText);
    await createErrorModal("Error", `There was a problem registering you as a voter: \n ${await resp.text()}`);
}



/**
 * Gets the current count of total voters registered, and updates
 * the element identified by targetQuery with that number.
 * @param {String} targetQuery query that identifies the element to update
 */
async function refreshRegisteredVoters(targetQuery) {
    const target = document.querySelector(targetQuery);
    const resp = await fetch(`/Vote/Voters`);
    const json = await resp.json();
    target.innerText = `${json.totalVoters}`;
}

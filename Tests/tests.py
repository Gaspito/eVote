import evote
import time
import threading


BASE = "http://localhost:5079" # base url to connect to
MAX_VOTES = 2 # maximum number of votes per Voter, should match app's launch settings


# Tests:

# --- Login & Access ---
# 1) Registration
# 2) Duplicate Email Registration
# 3) Login with valid Credentials
# 4) Login with invalid Credentials
# 5) Access Control
# 6) Default Role Assignement

# --- Votes ----
# 7) Candidate cannot vote
# 8) Voter can vote for candidate
# 9) Voter cannot vote for Voter
# 10) Voter cannot vote for themselves
# 11) Voter cannot vote for invalid user
# 12) Voter cannot cast duplicate vote
# 13) Voter cannot cast more than 2 votes

# --- Role Switch ---
# 14) Voter cannot change to candidate if they have votes
# 15) Voter can change to candidate if they have 0 votes
# 16) Candidate can change to voter if they have 0 votes
# 17) Candidate cannot change to voter if they have votes

# --- Demand Spikes ---
# 18) Only 2 vote requests from same voter can succeed
# 19) High volume test


def loginAndAccessTest():
    """Tests that user registration, login, logout and access works as intended"""
    user = evote.createUserSession(baseUrl=BASE)
    print("Test: Register new user")
    assert user is not None, "Failed to register a user with valid credentials"
    print("Test: Logout user")
    assert user.logout() == True, "Failed to logout user"
    print("Test: Login user")
    assert user.login() == True, "Failed to login user"
    print("Test: Logout user")
    assert user.logout() == True, "Failed to logout user"
    print("Test: Login user with invalid credentials")
    assert user.login(password="InvalidPassword1234!") == False, "User logged in with invalid credentials"
    print("Login and Access Tests all passed")
    print()

def votesTest():
    """Tests that vote casting works as inteded"""
    # TO DO: update this function so candidate users match the MAX_VOTE constant
    candidateUser = evote.createUserSession(baseUrl=BASE)
    candidateUser2 = evote.createUserSession(baseUrl=BASE)
    candidateUser3 = evote.createUserSession(baseUrl=BASE)
    voterUser = evote.createUserSession(baseUrl=BASE)
    assert candidateUser.switchToCandidate() == True, "Failed to create a candidate user"
    candidateUser2.switchToCandidate()
    candidateUser3.switchToCandidate()
    print("Test: Candidate cannot vote")
    assert candidateUser.addVote(candidateUser2.email) == False, "Candidate was able to case vote"
    print("Test: Voter can vote for candidate")
    assert voterUser.addVote(candidateUser.email) == True, "Voter could not cast a vote to candidate"
    print("Test: Voter cannot vote for themselves")
    assert voterUser.addVote(voterUser.email) == False, "Voter could cast a vote for themselves"
    print("Test: Voter cannot vote for an invalid user")
    assert voterUser.addVote("invalid@invalid.com") == False, "Voter could cast a vote for an invalid user"
    print("Test: Voter cannot cast duplicate votes")
    assert voterUser.addVote(candidateUser.email) == False, "Voter could cast a duplicate vote"
    print(f"Test: Voter cannot cast more than {MAX_VOTES} votes")
    assert voterUser.addVote(candidateUser2.email) == True
    assert voterUser.addVote(candidateUser3.email) == False, f"Voter could cast more than {MAX_VOTES} votes"

    print("Votes Tests all passed")
    print()

def roleSwitchTest():
    """Tests that switching from Voter to Candidate and vis-versa works as intended"""
    candidateUser = evote.createUserSession(baseUrl=BASE)
    voterUser = evote.createUserSession(baseUrl=BASE)
    assert candidateUser.switchToCandidate() == True, "Failed to create a candidate user"
    assert voterUser.addVote(candidateUser.email) == True, "Failed to cast vote to dummy candidate"
    print("Test: Voter cannot change to candidate if they have votes")
    assert voterUser.switchToCandidate() == False, "Voter was able to switch to candidate with cast votes"
    print("Test: Voter can change to candidate if they have 0 votes")
    assert voterUser.removeVote(candidateUser.email) == True, "Voter could not remove their vote"
    assert voterUser.switchToCandidate() == True, "Voter could not become a candidate"
    print("Test: Candidate can change to voter if they have 0 votes")
    assert candidateUser.switchToVoter() == True, "Candidate could not become a voter"
    print("Test: Candidate cannot change to voter if they have votes")
    assert voterUser.switchToVoter() == True, "Voter could not switch back"
    assert candidateUser.switchToCandidate() == True, "Candidate could not switch back"
    assert voterUser.addVote(candidateUser.email) == True, "Voter could not cast a vote"
    assert candidateUser.switchToVoter() == False, "Candidate became a voter while having votes"
    
    print("Role switching Tests all passed")
    print()

def demandSpikeTest():
    """Tests that multiple concurrent requests do not break intended behavior"""
    voterUser = evote.createUserSession(baseUrl=BASE)
    candidateUser = evote.createUserSession(baseUrl=BASE)
    assert candidateUser.switchToCandidate() == True, "Failed to create dummy candidate"
    voterEmail = voterUser.email
    voterPwd = "Test123!"

    def testThread(candidateEmail, voterUsername, tid):
        try:
            voter = evote.createLoggedOutUserSession(voterUsername, BASE)
            assert voter.login(voterPwd) == True, f"Failed to login (thread {tid})"
            voter.addVote(candidateEmail)
        except Exception as e:
            print(f"Exception during thread {tid}")
    
    _threads: list[threading.Thread] = []
    for i in range(5):
        dummyCandidate = evote.createUserSession(baseUrl=BASE)
        assert dummyCandidate.switchToCandidate() == True, "Failed to create dummy candidate"
        t = threading.Thread(target=testThread, args=(dummyCandidate.email, voterUser.username, i))
        t.start()
        _threads.append(t)
    
    for t in _threads:
        t.join()
    
    vote_count = voterUser.countVotersVotes()
    print(f"Vote count: {vote_count}")
    assert vote_count <= MAX_VOTES, f"More than {MAX_VOTES} votes were cast by the voter"
    
    print("Demand Spike Tests all passed")
    print()



loginAndAccessTest()
votesTest()
roleSwitchTest()
demandSpikeTest()
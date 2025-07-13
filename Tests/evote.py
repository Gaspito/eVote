import requests
import random
from bs4 import BeautifulSoup


class EVoteUser:
    """
    Utitility class abstracting a user and their possible interactions
    with the app
    """

    def __init__(self, session: requests.Session, username: str, baseUrl: str | None) -> None:
        self.username = username
        self.email = f"{username}@example.com"
        self.session: requests.Session = session
        self.baseUrl = baseUrl
    

    def logout(self) -> bool:
        """Attempts to log out the user"""
        session = self.session
        baseUrl = self.baseUrl

        # Get the login page to grab cookies and token
        login_page = session.get(f"{baseUrl}/Identity/Account/Manage")
        soup = BeautifulSoup(login_page.text, "html.parser")

        # Extract form action (in case it's in an Area)
        form = soup.find("form")
        action = "/Identity/Account/Logout"

        # Build payload from all inputs
        payload = {}
        for inp in form.find_all("input"):
            name = inp.get("name")
            if not name:
                continue

            # Grab the antiforgery token
            if name == "__RequestVerificationToken":
                payload[name] = inp["value"]
            
            # Grab nothing else

        # POST the form with the correct tokens
        response = session.post(f"{baseUrl}{action}", data=payload, allow_redirects=False)

        # A successful attempt should redirect
        if response.status_code not in (301, 302, 201):
            print(f"Failed to logout user: {response.status_code} - {response.text}")
            return False
        
        return True


    def login(self, password: str="Test123!") -> bool:
        """Attempts to login the user, with an optional different password"""
        session = self.session
        baseUrl = self.baseUrl

        # Get the login page to grab cookies and token
        login_page = session.get(f"{baseUrl}/Identity/Account/Login")
        soup = BeautifulSoup(login_page.text, "html.parser")

        # Extract form action (in case it's in an Area)
        form = soup.find("form")
        action = "/Identity/Account/Login"

        # Build payload from all inputs
        payload = {}
        for inp in form.find_all("input"):
            name = inp.get("name")
            if not name:
                continue

            # Grab the antiforgery token
            if name == "__RequestVerificationToken":
                payload[name] = inp["value"]

            # Fill in required fields by matching their name
            elif name.endswith(".Email") or name == "Email":
                payload[name] = self.email
            elif name.endswith(".Password") or name == "Password":
                payload[name] = password

            # Preserve any other hidden/default values
            else:
                payload[name] = inp.get("value", "")

        # POST the form with correct fields and hidden values
        response = session.post(f"{baseUrl}{action}", data=payload, allow_redirects=False)

        # A successful attempt should redirect
        if response.status_code not in (301, 302, 201):
            print(f"Failed to login user: {response.status_code}")
            return False
        
        return True


    def switchToCandidate(self) -> bool:
        """Attempts to switch the user's role to candidate"""
        response = self.session.put(f"{self.baseUrl}/Vote/RegisterAsCandidate")
        if response.status_code == 200:
            return True
        else:
            print(response.status_code, response.text)
            return False


    def switchToVoter(self) -> bool:
        """Attempts to switch the user's role to voter"""
        response = self.session.put(f"{self.baseUrl}/Vote/RegisterAsVoter")
        if response.status_code == 200:
            return True
        else:
            print(response.status_code, response.text)
            return False


    def addVote(self, candidate: str | None=None) -> bool:
        """Attempts to cast a vote for the given candidate (by email)"""
        if candidate is None:
            raise ValueError("Missing candidate email")
        response = self.session.post(f"{self.baseUrl}/Vote/AddVote", json={"Email": candidate})
        # handle the case of the 'unauthorized' response, that still give 200 status code, 
        # but returns html and not json
        if response.status_code == 200 and not response.text.startswith("<!DOCTYPE html>"):
            return True
        else:
            return False


    def removeVote(self, candidate=None):
        """Attempts to remove a vote for the given candidate (by email)"""
        if candidate is None:
            raise ValueError("Missing candidate email")
        response = self.session.delete(f"{self.baseUrl}/Vote/RemoveVote", json={"Email": candidate})
        # handle the case of the 'unauthorized' response, that still give 200 status code, 
        # but returns html and not json
        if response.status_code == 200 and not response.text.startswith("<!DOCTYPE html>"):
            return True
        else:
            return False
    

    def countCandidateVotes(self) -> int:
        """Attempts to get the number of votes for this user (as a Candidate)"""
        response = self.session.post(f"{self.baseUrl}/Vote/Count", json={"Email": self.email})
        if response.status_code == 200:
            return response.json()["votes"]
        else:
            raise RuntimeError(f"Failed to count voutes: {response.status_code} - {response.text}")
    

    def countVotersVotes(self) -> int:
        """Attempts to get the number of votes cast by this user (as a Voter)"""
        response = self.session.get(f"{self.baseUrl}/Vote/Votes")
        if response.status_code == 200:
            return len(response.json()["votes"])
        else:
            raise RuntimeError(f"Failed to count voutes: {response.status_code} - {response.text}")


def createLoggedOutUserSession(username, baseUrl=None):
    """Creates a user based on username, without logging them in"""
    session = requests.Session()
    user = EVoteUser(session, username, baseUrl)
    return user

def createUserSession(username=None, baseUrl=None):
    """Creates and registers a new user, with an optional username"""
    if baseUrl is None:
        raise ValueError("Missing Base URL")
    
    # create a random username if not provided
    if username is None:
        username = f"testUser{random.randint(0, 44100)}"

    session = requests.Session()

    # Get the login page to grab cookies and token
    login_page = session.get(f"{baseUrl}/Identity/Account/Register")
    soup = BeautifulSoup(login_page.text, "html.parser")

    # Extract form action (in case it's in an Area)
    form = soup.find("form")
    action = form["action"] or "/Identity/Account/Register"

    # Build payload from all inputs
    payload = {}
    for inp in form.find_all("input"):
        name = inp.get("name")
        if not name:
            continue

        # Grab the antiforgery token
        if name == "__RequestVerificationToken":
            payload[name] = inp["value"]

        # Fill in required fields by matching their name
        elif name.endswith(".Email") or name == "Email":
            payload[name] = f"{username}@example.com"
        elif name.endswith(".Password") or name == "Password":
            payload[name] = "Test123!"
        elif name.endswith(".ConfirmPassword"):
            payload[name] = "Test123!"

        # Preserve any other hidden/default values
        else:
            payload[name] = inp.get("value", "")

    # POST the form
    r2 = session.post(f"{baseUrl}{action}", data=payload, allow_redirects=False)

    if r2.status_code not in (301, 302, 201):
        raise RuntimeError(f"Failed to register new user: {r2.status_code} - {r2.text}")

    user = EVoteUser(session, username, baseUrl)

    return user


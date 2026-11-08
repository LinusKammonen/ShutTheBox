import { withLoading } from "./loading.js";

const form = document.querySelector("#loginForm");
const usernameInput = document.querySelector("#username");
const loginError = document.querySelector("#loginError");

form.addEventListener("submit", async (event) => {
    event.preventDefault();
    loginError.textContent = "";

    try {
        const response = await withLoading("Loggar in…", () => fetch("/v1/auth/login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username: usernameInput.value })
        }), { blocking: true, target: ["#loginForm button[type=submit]"] });

        const data = await response.json();

        if (!response.ok) {
            loginError.textContent = data.detail ?? "Inloggningen misslyckades.";
            return;
        }

        localStorage.setItem("token", data.token);
        window.location.replace("/index.html");
    }
    catch (error) {
        loginError.textContent = "Kunde inte nå servern.";
    }
});

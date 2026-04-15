document.getElementById("login-form").addEventListener("submit", async (e) => {
  e.preventDefault();
  const email = document.getElementById("email").value.trim();
  const password = document.getElementById("password").value;
  const err = document.getElementById("error");
  err.hidden = true;

  if (!email || !password) {
    err.textContent = "Email and password required.";
    err.hidden = false;
    return;
  }

  try {
    const session = await Auth.signIn({ email });
    window.location.href = `${session.role}.html`;
  } catch (e) {
    err.textContent = e.message || "Sign in failed.";
    err.hidden = false;
  }
});

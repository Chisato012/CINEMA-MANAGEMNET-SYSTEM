(() => {
  const root = document.querySelector(".cosmos-chat");
  if (!root) return;

  const panel = root.querySelector(".cosmos-chat-panel");
  const toggle = root.querySelector(".cosmos-chat-toggle");
  const close = root.querySelector(".cosmos-chat-close");
  const form = root.querySelector(".cosmos-chat-form");
  const input = root.querySelector(".cosmos-chat-input");
  const messages = root.querySelector(".cosmos-chat-messages");
  const apiUrl = root.dataset.chatApi || "http://localhost:5218/api/chat";
  const userId = root.dataset.userId ? Number(root.dataset.userId) : null;

  const appendMessage = (text, type) => {
    const bubble = document.createElement("div");
    bubble.className = `cosmos-chat-message ${type}`;
    bubble.textContent = text;
    messages.appendChild(bubble);
    messages.scrollTop = messages.scrollHeight;
    return bubble;
  };

  toggle.addEventListener("click", () => {
    panel.hidden = false;
    toggle.hidden = true;
    input.focus();
  });

  close.addEventListener("click", () => {
    panel.hidden = true;
    toggle.hidden = false;
  });

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    const message = input.value.trim();
    if (!message) return;

    appendMessage(message, "user");
    input.value = "";
    input.disabled = true;

    const pending = appendMessage("Mình đang tìm suất chiếu phù hợp...", "bot");

    try {
      const response = await fetch(apiUrl, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ message, userId }),
      });

      if (!response.ok) {
        throw new Error(`Chat API error ${response.status}`);
      }

      const data = await response.json();
      pending.textContent = data.answer || "Mình chưa có câu trả lời phù hợp.";
    } catch {
      pending.textContent =
        "Mình chưa kết nối được chatbot API. Hãy kiểm tra Chatbot/Customer có đang chạy ở http://localhost:5218 không nhé.";
    } finally {
      input.disabled = false;
      input.focus();
    }
  });
})();

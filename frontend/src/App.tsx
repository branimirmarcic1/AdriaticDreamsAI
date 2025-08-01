import { useState, useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import './App.css';

// Definicija tipova za bolju organizaciju koda
type Message = {
  text: string;
  sender: 'user' | 'bot';
};

const API_URL = "http://localhost:8080";

function App() {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [messages, setMessages] = useState<Message[]>([]);
  const [inputText, setInputText] = useState('');

  // Koristimo useRef da izbjegnemo probleme s closure u SignalR event handleru
  const latestMessages = useRef<Message[]>([]);
  latestMessages.current = messages;

  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/chathub`)
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, []);

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => console.log('SignalR Connected!'))
        .catch(e => console.log('Connection failed: ', e));

      connection.on('ReceiveAnswer', (answer: string) => {
        const botMessage: Message = { text: answer, sender: 'bot' };
        // Koristimo ref za pristup najnovijem stanju poruka
        const updatedMessages = [...latestMessages.current, botMessage];
        setMessages(updatedMessages);
      });
    }

    // Čistimo konekciju kada se komponenta uništi
    return () => {
      connection?.stop();
    };
  }, [connection]);

  const sendMessage = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (inputText.trim() === '') return;

    const userMessage: Message = { text: inputText, sender: 'user' };
    setMessages([...messages, userMessage]);

    try {
      await fetch(`${API_URL}/Chat/ask`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(inputText)
      });
    } catch (error) {
      console.error("Error sending message:", error);
    }

    setInputText('');
  };

  return (
    <div className="app">
      <div className="chat-window">
        {messages.map((msg, index) => (
          <div key={index} className={`message ${msg.sender}`}>
            {msg.text}
          </div>
        ))}
      </div>
      <form onSubmit={sendMessage} className="input-form">
        <input
          type="text"
          value={inputText}
          onChange={(e) => setInputText(e.target.value)}
          placeholder="Postavite pitanje..."
        />
        <button type="submit">Pošalji</button>
      </form>
    </div>
  );
}

export default App;
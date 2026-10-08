const usuario = window.location.pathname.split("/").pop();
const connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").withAutomaticReconnect().build();
const followCounter = document.getElementById("followCounter");
const subCounter = document.getElementById("subCounter");

//Listener del cliente
connection.on("followers", message => {
    console.log(message);

    let f = message["followers"];
    let s = message["subs"];

    followCounter.textContent = f;
    subCounter.textContent = s;
    //createMessage(message);
});



connection.onreconnecting(error => {
    console.log("Reconectando...", error);
});

connection.onreconnected(connectionId => {
    console.log("Reconectado:", connectionId);
});

connection.onclose(error => {
    console.log("Conexión cerrada:", error);
});


//Conexion al socket SignalR
async function Connect() {
    while (connection.state === signalR.HubConnectionState.Disconnected) {
        try {
            await connection.start();
            console.log("Conectado");
            break;
        } catch (error) {
            console.error("Error:", error);
            await new Promise(resolve => setTimeout(resolve, 3000));
        }
    }

    if (connection.state === signalR.HubConnectionState.Connected) {
        while (connection.state === signalR.HubConnectionState.Connected) {
            try {
                await connection.invoke("Join", usuario);
                console.log("Join enviado");
                break;
            } catch (error) {
                console.error("Error Join:", error);
                await new Promise(resolve => setTimeout(resolve, 3000));
            }
        }
    }


}

async function GetFollows() {
    const response = await fetch(`https://botserver-qccm.onrender.com/data/${usuario}`);
    const data = await response.json();

    followCounter.textContent = data["followers"];
    subCounter.textContent = data["subs"];
    console.log(data);
}



GetFollows().catch(err => console.error(err));

//Captura de errores
Connect().catch(err => console.error(err));

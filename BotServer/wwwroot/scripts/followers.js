const usuario = window.location.pathname.split("/").pop();
const connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").withAutomaticReconnect().build();
const followCounter = document.getElementById("followCounter");
const subCounter = document.getElementById("subCounter");

let conectando = false;

async function JoinChannel() {
    while (connection.state === signalR.HubConnectionState.Connected) {
        try {
            await connection.invoke("Join", usuario);

            console.log("Unido al canal:", usuario);
            return;
        } catch (error) {
            console.error("Error al unirse:", error);
            await sleep(3000);
        }
    }
}

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
    console.warn("SignalR reconectando:", error);
});

connection.onreconnected(async connectionId => {
    console.log("SignalR reconectado:", connectionId);

    await JoinChannel();
});

connection.onclose(error => {
    console.log("Conexión cerrada:", error);
});


//Conexion al socket SignalR
async function Connect() {
    if (conectando) return;

    conectando = true;

    try {
        while (connection.state === signalR.HubConnectionState.Disconnected) {
            try {
                await connection.start();

                console.log("SignalR conectado");

                await JoinChannel();
                return;
            } catch (error) {
                console.error("Error de conexión:", error);
                await sleep(3000);
            }
        }
    } finally {
        conectando = false;
    }
}

async function GetFollows() {

    //const response = await fetch(`http://localhost:8000/data/${usuario}`);
    const response = await fetch(`https://botserver-qccm.onrender.com/data/${usuario}`);
    const data = await response.json();

    followCounter.textContent = data["followers"];
    subCounter.textContent = data["subs"];
    console.log(data);
}



GetFollows().catch(err => console.error(err));

//Captura de errores
Connect().catch(err => console.error(err));

const usuario = window.location.pathname.split("/").pop();
const connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").withAutomaticReconnect().build();

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

connection.on("win", () => {
    increaseValue("win");
});


connection.on("lose", () => {
    increaseValue("lose");
});

connection.on("nowin", () => {
    decreaseValue("nowin");
});

connection.on("nolose", () => {
    decreaseValue("nolose");
});

connection.on("reset", () => {
    resetValue("reset");
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


function updateRate(){
    var wins = document.getElementById("win");
    var lose = document.getElementById("lose");

    var winsInt = parseFloat(wins.textContent,10);
    var loseInt = parseFloat(lose.textContent,10);

    var total = winsInt + loseInt;

    var rate = (winsInt / total) * 100;

    var r = document.getElementById("rate");
    r.textContent = rate.toFixed(2);
}


function increaseValue(data) {
    let val;
    
    if (data == "win") {
        val = document.getElementById("win");
    } else if(data == "lose") {
        val = document.getElementById("lose");
    }

    var value = parseInt(val.textContent, 10);
    value += 1;

    val.textContent = value;
    updateRate();


}

function decreaseValue(data) {

    let val;
    data = JSON.parse(data);
    if (data["content"] == "win") {
        val = document.getElementById("win");
    } else if (data["content"] == "lose") {
        val = document.getElementById("lose");
    }

    var value = parseInt(val.textContent, 10);

    if (value > 0) {
        value -= 1;

        val.textContent = value;
    }
    updateRate();

}

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


function resetValue(data) {

    var values = document.querySelectorAll(".value");

    values.forEach(element => {
        element.textContent = 0;
    });
    updateRate()

}



Connect().catch(err => console.error(err));
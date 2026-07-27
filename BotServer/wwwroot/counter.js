const usuario = window.location.pathname.split("/").pop();
const connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").withAutomaticReconnect().build();



connection.on("win", () => {
    increaseValue(message);
});


connection.on("lose", () => {
    increaseValue(message);
});

connection.on("nowin", () => {
    decreaseValue(message);
});

connection.on("nolose", () => {
    decreaseValue(message);
});

connection.on("reset", () => {
    resetValue(message);
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
    await connection.start();
    console.log("Conectado");
    await connection.invoke("Join", usuario)
    console.log("Join enviado");

    //setTimeout(async () => { await connection.invoke("SendToClient", "carlos", "Hola nuevo usuario") }, 2000);
}


function resetValue(data) {

    var values = document.querySelectorAll(".value");

    values.forEach(element => {
        element.textContent = 0;
    });
    updateRate()

}



Connect().catch(err => console.error(err));
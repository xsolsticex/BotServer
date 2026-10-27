
const usuario = window.location.pathname.split("/").pop();
const contenedor = document.getElementById("container");
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

//Listener del cliente
connection.on("botMessage", message => {
    createMessage(message);
});


connection.on("ColorChanged", message => {
    console.log(message);

    if (CSS.supports("color", message)) {

        document.documentElement.style.setProperty("--border-message", message);
    } else {
        document.documentElement.style.setProperty("--border-message", "#66b2ff");
    }


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






//Captura de errores
Connect().catch(err => console.error(err));



//NEW SOCKET FUNCTIONS


setInterval(() => {

    var size = contenedor.children.length;

    if (size > 5) {
        var child = contenedor.firstElementChild;
        child.classList.add("removing");
        setTimeout(() => {
            child.remove();
        }, 1000); // duración de la animación
    }
}, 3600);


function createMessage(data) {


    var username = data["username"];
    var content = data["content"];
    var color = data["color"];
    var profile = data["profile"];
    var badges = data["badges"];


    var mensaje = document.createElement("div")

    mensaje.classList.add("mensaje");

    if (badges.length == 1) {
        mensaje.innerHTML = `
    <div class="sect-1">
        <img src="${profile}">
    </div>
    <div class="sect-2">
    <div class='subheader'>
           <span class='badges'>
              <img src='${badges[0]}'/>
       </span>
       <span style='color: ${color}'>${username}</span>
    </subheader>

       <div>
          ${content}
       </div>
    </div>`
    } else if (badges.length == 2) {
        mensaje.innerHTML = `
    <div class="sect-1">
        <img src="${profile}">
    </div>
    <div class="sect-2">
    <div class='subheader'>
        <div class='top'>
            <span class='badges'>
                <img src='${badges[0]}'/>
                <img src='${badges[1]}'/>
            </span>
            <span style='color: ${color}'>${username}</span>
        </div>
    </div>
    <div>
        ${content}
    </div>
    </div>`
    } else if (badges.length == 3) {
        mensaje.innerHTML = `
    <div class="sect-1">
        <img src="${profile}">
    </div>
    <div class="sect-2">
    <div class='subheader'>
        <div class='top'>
            <span class='badges'>
                <img src='${badges[0]}'/>
                <img src='${badges[1]}'/>
                <img src='${badges[2]}'/>
            </span>
            <span style='color: ${color}'>${username}</span>
        </div>
    </div>
    <div>
        ${content}
    </div>
    </div>`
    } else if (badges.length == 4) {
        mensaje.innerHTML = `
    <div class="sect-1">
        <img src="${profile}">
    </div>
    <div class="sect-2">
    <div class='subheader'>
        <div class='top'>
            <span class='badges'>
                <img src='${badges[0]}'/>
                <img src='${badges[1]}'/>
                <img src='${badges[2]}'/>
                <img src='${badges[3]}'/>
            </span>
            <span style='color: ${color}'>${username}</span>
        </div>
    </div>
    <div>
        ${content}
    </div>
    </div>`
    } else {
        mensaje.innerHTML = `
    <div class="sect-1">
        <img src="${profile}">
    </div>
    <div class="sect-2">
    <div class='subheader'>
        <div class='top'>
            <span style='color: ${color}'>${username}</span>
        </div>
    </div>
    <div>
        ${content}
    </div>
    </div>`
    }



    contenedor.appendChild(mensaje);
    // Eliminar tras 10 segundos
    setTimeout(() => {
        mensaje.classList.add("removing");

        setTimeout(() => {
            mensaje.remove();
        }, 1000); // duración animación
    }, 60 * 1000);

    console.log("");

}


const usuario = window.location.pathname.split("/").pop();
const contenedor = document.getElementById("container");
const connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").withAutomaticReconnect().build();



//Listener del cliente
connection.on("botMessage", message => {
    createMessage(message);
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
    await connection.start();
    console.log("Conectado");
    await connection.invoke("Join", usuario)
    console.log("Join enviado");

    //setTimeout(async () => { await connection.invoke("SendToClient", "carlos", "Hola nuevo usuario") }, 2000);
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

    var mensaje = document.createElement("div")

    mensaje.classList.add("mensaje");
    mensaje.innerHTML = `
    <div class="sect-1">
                <img
                    src="${profile}">
            </div>
            <div class="sect-2">
                <span style='color: ${color}'>${username}</span>
                <div>
                    ${content}
                </div>
            </div>`

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

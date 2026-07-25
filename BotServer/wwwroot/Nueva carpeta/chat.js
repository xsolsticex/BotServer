import { createSocket } from "./socket.js";

const contenedor = document.getElementById("container");





let socket;

function connect() {

    socket = createSocket();

    socket.onmessage = (event) => {
        var data = event.data;
        data = JSON.parse(data);
        if (data["type"] == "message") {
            createMessage(data);
        }



    }


    socket.onopen = () => {
        console.log("Conectado");

    };

    socket.onclose = () => {
        console.log("Desconectado. Reintentando...");
        setTimeout(connect, 5000);
    };


    socket.onerror = (error) => {
        console.error("Error:", error);
        socket.close();
    };

}


// var height = contenedor.getBoundingClientRect();

function sendMessage() {
    socket.send("Mensaje desde cliente")
}

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
    }, 60*1000);

}


connect();
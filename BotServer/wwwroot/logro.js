const msg = document.getElementById("message");
const logro = document.getElementById("logro");
const connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").withAutomaticReconnect().build();
const data = document.getElementById("data");
const image = document.getElementById("logo");
const logroContent = document.getElementById("logroContent");
const sound = new Audio("https://botserver-qccm.onrender.com/achievement.mp3");
const usuario = window.location.pathname.split("/").pop();

console.log(usuario);

//Listener del cliente
connection.on("newFollow", message => {

    if (message != null) {
        let user = message["follow"];


        openMessage(user);
        //message.textContent = user;
        

    }

//createMessage(message);
});

function openMessage(message) {

    sound.currentTime = 0;
    logroContent.classList.add("shown");
    setTimeout(() => {
        sound.play();
        image.classList.add("rotation");
        logro.classList.remove("closed");
    }, 2000);

    setTimeout(() => {

        image.classList.remove("rotation");
        data.classList.remove("closed");
    }, 3200)

    msg.textContent = message;


    setTimeout(() => { closeMessage(); }, 6000);
}



async function GetLogo(usuario) {
    const response = await fetch(`https://botserver-qccm.onrender.com/profile/${usuario}`);

    if (!response.ok) {
        throw new Error("Error al obtener el perfil");
    }

    const data = await response.json();

    image.src = data["profile"];


}

function closeMessage() {

    image.classList.add("rotation-reverse");

    data.classList.add("closed");

    setTimeout(() => {

        logro.classList.add("closed");
        image.classList.remove("rotation-reverse");

    }, 1000);

    setTimeout(() => { logroContent.classList.remove("shown"); }, 2000);


}


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





//Captura de errores
Connect().catch(err => console.error(err));


GetLogo(usuario);
//openMessage("xhipicolgaox");




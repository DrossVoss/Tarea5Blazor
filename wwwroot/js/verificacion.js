window.tarea5 = (() => {
    let conexion = null;
    let confirmando = false;

    async function enviarJson(url, datos) {
        const respuesta = await fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(datos)
        });
        if (!respuesta.ok) {
            throw new Error(await respuesta.text());
        }
        return respuesta;
    }

    return {
        async crear(nombre, email) {
            const respuesta = await enviarJson("/api/registro", { nombre, email });
            return (await respuesta.json()).id;
        },

        async escuchar(id, referenciaBlazor) {
            if (conexion) await conexion.stop();
            confirmando = false;
            const actual = new signalR.HubConnectionBuilder()
                .withUrl("/verificacionHub")
                .withAutomaticReconnect()
                .build();
            conexion = actual;

            async function avisar() {
                if (confirmando) return;
                confirmando = true;
                await referenciaBlazor.invokeMethodAsync("Confirmado");
            }

            actual.on("VerificacionCompletada", avisar);
            actual.onreconnected(async () => {
                await actual.invoke("Unirse", id);
                if (await actual.invoke("EstaVerificada", id)) await avisar();
            });

            await actual.start();
            await actual.invoke("Unirse", id);
            if (await actual.invoke("EstaVerificada", id)) await avisar();
        },

        async finalizar(id) {
            await enviarJson("/api/finalizar", { id });
            window.location.replace("/");
        },

        async salir() {
            await enviarJson("/api/salir", {});
            window.location.replace("/registro");
        },

        async desconectar() {
            if (conexion) {
                const anterior = conexion;
                conexion = null;
                await anterior.stop();
            }
        }
    };
})();

const common = {
    defaultLanguage: "kz",
    siteLanguages: ["kz", "ky", "ru", "en", "zh-cn", "tr"],
    formatTime: seconds => {
        seconds = Math.max(0, Math.floor(seconds));
        const minutes = Math.floor(seconds / 60);
        const remainingSeconds = seconds % 60;
        const formattedMinutes = String(minutes).padStart(2, "0");
        const formattedSeconds = String(remainingSeconds).padStart(2, "0");
        return `${formattedMinutes}:${formattedSeconds}`;
    },
    copy: async text =>{
        const $toast = $("#copy-toast");
        const $toastBody = $toast.find(".toast-body");
        const successText = $toastBody.attr("data-success-text");
        const errorText = $toastBody.attr("data-error-text");
        if (navigator && navigator.clipboard) {
            navigator.clipboard.writeText(text).then(r => {});
            $toastBody.text(successText);
            const toast = new bootstrap.Toast($toast[0]);
            toast.show();
        } else {
            const textarea = document.createElement("textarea");
            textarea.value = text;
            textarea.style.position = "fixed";
            textarea.style.opacity = "0";
            document.body.appendChild(textarea);
            textarea.focus();
            textarea.select();
            try {
                document.execCommand("copy");
                $toastBody.text(successText);
                const toast = new bootstrap.Toast($toast[0]);
                toast.show();
            } catch {
                $toastBody.text(errorText);
                const toast = new bootstrap.Toast($toast[0]);
                toast.show();
            } finally {
                document.body.removeChild(textarea);
            }
        }
    },
}
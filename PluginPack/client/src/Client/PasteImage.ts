
export async function PasteImageIntoDocument(file: File) {
  var pasteResult = Promise.withResolvers<void>();
  const reader = new FileReader();
  reader.onload = async (ev) => {
    try {
      if (ev.target && ev.target.result) {
        const blob = new Blob([ev.target.result], { type: file.type });
        const fd = new FormData();
        fd.append("image", blob, "./img/" + file.name);
        await fetch('http://api.example/paste-image', { method: "POST", body: fd });
      }
      pasteResult.resolve();
    }
    catch (err) {
      pasteResult.reject(err);
    }
  };
  reader.readAsDataURL(file);
  await pasteResult.promise;
}

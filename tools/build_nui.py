import os, re

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
MENU_HTML = r"C:/Users/lucky/OneDrive/Desktop/[fsrp]/fsrp-nativeui-nui/nui/index.html"
IMAGES_DIR = r"C:/Users/lucky/OneDrive/Desktop/[fsrp]/fsrp-nativeui-nui/images"
STORAGE = os.path.join(REPO, "vMenuServer", "storage.html")
OUT_DIR = os.path.join(REPO, "build", "vMenu", "nui")
OUT = os.path.join(OUT_DIR, "index.html")
OUT_IMAGES = os.path.join(REPO, "build", "vMenu", "images")

STORAGE_CSS = """
@import url('https://fonts.googleapis.com/css2?family=Roboto&display=swap');
#vmenu-storage * { margin:0; padding:0; text-align:center; box-sizing:border-box; font-family:'Roboto',sans-serif; }
#vmenu-storage { position:absolute; inset:0; z-index:50; pointer-events:none; }
#vmenu-storage #body { margin-top:50px; max-width:1200px; margin-left:auto; margin-right:auto; background-color:rgba(255,255,255,0.9); border-radius:8px; pointer-events:auto; }
#vmenu-storage .error { background-color:lightcoral; color:rgb(121,34,34); padding:30px; }
#vmenu-storage input[type="checkbox"]:not(:checked)+div { display:none; }
#vmenu-storage hr { margin-top:10px; margin-bottom:10px; border:none; border-top:1px solid lightgray; }
#vmenu-storage button:not(#close-button) { padding:5px 10px; border:none; border-radius:3px; background-color:#0075ff; font-size:14px; color:white; }
#vmenu-storage #close-button { position:relative; top:0; right:5px; background:none; border:none; float:right; font-size:20px; font-weight:100; color:gray; }
#vmenu-storage .grid { display:grid; grid-template-columns:repeat(2,1fr); }
#vmenu-storage .column { padding:20px; }
#vmenu-storage .column:first-of-type { border-right:1px solid lightgray; }
#vmenu-storage textarea { width:100%; height:100%; text-align:left; padding:5px; }
#vmenu-storage strong { color:rgb(189,45,45); }
#vmenu-storage .mt { display:block; margin-top:8px; }
#vmenu-storage .mb { display:block; margin-bottom:8px; }
#vmenu-storage .left { text-align:left; }
#vmenu-storage ol, #vmenu-storage li { text-align:left; }
#vmenu-storage ol { margin-left:20px; }
#vmenu-storage input[type=file] { margin-top:8px; margin-bottom:8px; }
"""


def extract(storage_html):
    body = re.search(r"<body>(.*)</body>", storage_html, re.S).group(1)
    script_m = re.search(r"<script>(.*?)</script>", body, re.S)
    script = script_m.group(1)
    markup = body[: script_m.start()].strip()
    # ignore our own menu messages so the storage panel does not pop up on every frame
    script = script.replace(
        'window.addEventListener("message", (data) => {',
        'window.addEventListener("message", (data) => {\n      if (!data.data || data.data.action) return;',
    )
    return markup, script


def main():
    with open(MENU_HTML, encoding="utf-8") as fh:
        html = fh.read()
    with open(STORAGE, encoding="utf-8-sig") as fh:
        storage = fh.read()

    markup, script = extract(storage)

    head_inject = "<style>" + STORAGE_CSS + "</style>"
    body_inject = (
        '<div id="vmenu-storage">' + markup + "</div>"
        + "<script>" + script + "</script>"
    )

    html = html.replace("</head>", head_inject + "</head>", 1)
    html = html.replace("</body>", body_inject + "</body>", 1)

    os.makedirs(OUT_DIR, exist_ok=True)
    with open(OUT, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(html)

    os.makedirs(OUT_IMAGES, exist_ok=True)
    for name in ("banner.png", "logo.png"):
        src = os.path.join(IMAGES_DIR, name)
        if os.path.exists(src):
            with open(src, "rb") as a, open(os.path.join(OUT_IMAGES, name), "wb") as b:
                b.write(a.read())

    print("wrote", OUT)
    print("images ->", OUT_IMAGES)


if __name__ == "__main__":
    main()

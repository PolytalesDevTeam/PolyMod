import os
import PyInstaller.__main__

ROOT = os.path.abspath(os.getcwd())

PyInstaller.__main__.run(
    [
        "--noconfirm",
        "--onefile",
        "--windowed",
        "--icon",
        ROOT + "/icon.ico",
        "--name",
        "PolyMod",
        "--add-data",
        ROOT + "/icon.ico:.",
        ROOT + "/main.py",
    ]
)

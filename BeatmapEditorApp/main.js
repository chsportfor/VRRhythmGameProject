const { app, BrowserWindow } = require('electron');
const path = require('path');

function createWindow() {
    const win = new BrowserWindow({
        width: 1400,
        height: 900,
        title: "VR Rhythm Game - 프리미엄 채보 에디터",
        webPreferences: {
            nodeIntegration: true,
            contextIsolation: false
        }
    });

    // 상단 메뉴바 숨기기 (더 깔끔한 프로그램 모양을 제공)
    win.setMenuBarVisibility(false);

    // 같은 폴더 내의 BeatmapEditor.html 파일을 로드
    win.loadFile(path.join(__dirname, 'BeatmapEditor.html'));
}

app.whenReady().then(() => {
    createWindow();

    app.on('activate', () => {
        if (BrowserWindow.getAllWindows().length === 0) {
            createWindow();
        }
    });
});

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') {
        app.quit();
    }
});

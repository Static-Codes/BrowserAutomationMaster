SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
APP_EXECUTABLE_NAME="bamm"
APP_EXECUTABLE_PATH="$SCRIPT_DIR/$APP_EXECUTABLE_NAME"
chmod +x "$APP_EXECUTABLE_PATH"
osascript -e 'tell application "Terminal" to do script "cd \"'$SCRIPT_DIR'\" && \"'$APP_EXECUTABLE_PATH'\""'
exit 0
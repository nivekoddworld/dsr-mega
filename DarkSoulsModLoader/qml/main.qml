import QtQuick
import QtQuick.Controls
import QtQuick.Layouts

ApplicationWindow {
    id: root
    visible: true
    width: 900
    height: 700
    title: "Dark Souls Mod Loader"

    ColumnLayout {
        anchors.fill: parent
        anchors.margins: 16
        spacing: 16

        // Tab navigation
        TabBar {
            id: tabBar
            Layout.fillWidth: true

            TabButton {
                text: "Mods"
            }
            TabButton {
                text: "Randomizer"
            }
            TabButton {
                text: "Launch"
            }
            TabButton {
                text: "Settings"
            }
        }

        // Content area
        StackLayout {
            currentIndex: tabBar.currentIndex
            Layout.fillWidth: true
            Layout.fillHeight: true

            // Mods Page
            Rectangle {
                color: "#f0f0f0"
                Text {
                    anchors.centerIn: parent
                    text: "Mods Page"
                }
            }

            // Randomizer Page
            Rectangle {
                color: "#f0f0f0"
                Text {
                    anchors.centerIn: parent
                    text: "Randomizer Page"
                }
            }

            // Launch Page
            Rectangle {
                color: "#f0f0f0"
                Text {
                    anchors.centerIn: parent
                    text: "Launch Page"
                }
            }

            // Settings Page
            Rectangle {
                color: "#f0f0f0"
                Text {
                    anchors.centerIn: parent
                    text: "Settings Page"
                }
            }
        }
    }
}

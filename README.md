# QuickUp Tool

QuickUp Tool is a lightweight application designed to quickly upload files for free to a file provider.

## Features

- **Drag and Drop Uploads:** Simply drag and drop files to upload.
- **Progress Tracking:** Visualize upload progress with the progress bar.
- **History Management:** Keep track of previously uploaded files.
- **Clipboard Integration:** Automatically copies the uploaded file's URL to the clipboard.

## Download
- Windows Store link pending

## Build Guide

### Prerequisites

- Windows 10 or later
- Visual Studio 2019 or later

### Installation

1. Clone the repository:
    ```sh
    git clone https://github.com/megabytesme/QuickUp-Tool.git
    ```
2. Open the project in Visual Studio.
3. Build the solution.

### Running the Application

1. Start the application by pressing `F5` or by selecting `Debug > Start Debugging`.
2. Drag and drop a file into the application to upload.

### Usage

1. **Drag and Drop:** Drag files directly into the application window.
2. **Upload Progress:** Monitor the upload progress via the progress bar.
3. **History:** View upload history in the main window.
4. **Clipboard:** The URL of the uploaded file is copied automatically.

## Folder Structure

- `1709 UWP`: UWP app implementation which supports devices on Windows 1709 and above (looking at you W10M) - Uses UWP WinUI. Recommended for Windows 10 Mobile users.
- `1809 UWP`: UWP app implementation which supports devices on Windows 1809 and above - Uses WinUI 2. Recommended for all Windows device users.
- `WinUI 3`: Packaged WinUI 3 app implementation which supports Desktop Windows 1809 and above - Uses WinUI 3 of course. Recommended for all Windows Desktop users.
- `Shared Code`: Project which holds the code shared by all projects.

## Contributing

1. Fork the repository.
2. Create a new branch (`git checkout -b feature-branch`).
3. Commit your changes (`git commit -m 'Add new feature'`).
4. Push to the branch (`git push origin feature-branch`).
5. Create a new Pull Request.

## License

This project is licensed under the CC BY-NC-SA 4.0 License - see the [LICENSE](LICENSE.md) file for details.



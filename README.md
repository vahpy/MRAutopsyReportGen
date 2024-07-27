# MRAutopsyReportGen
This repository contains code for the research paper titled "Collaborative Forensic Autopsy Documentation and Supervised Report Generation using a Hybrid Mixed-Reality Environment and Generative AI", which will be published in IEEE Transactions on Visualization and Computer Graphics (ISMAR 2024), DOI: --will be updated once published --  [access author-version manuscript](https://doi.org/10.31219/osf.io/y9q85)

# How to use
The project was built on Unity 2021.3.19fa and Mixed Reality Toolkit (MRTK) 2.8.2.

Steps to run the project:
1. Open the `unityproject` directory using Unity.
2. Go to `Volume Rendering` in the menu bar and select `Import`. Import your dataset using the appropriate option.
3. Add the imported object as a child of the existing `VolumeRenderedObject`.
4. Select `VolumeRenderedObject` and toggle the `lighting` option.
5. Adjust the transform of `VolumeContainer` to centre the object within `VolumeRenderedObject`.
6. Find `config = SpeechConfig.FromSubscription("", "australiaeast");` in `unityproject/Assets/Scripts/Record/Audio/Dictation/AzureSpeechToText.cs` and change it based on your Azure Speech Recognition Subscription Key.
7. In the first lines of `webapp/js/generatedstructuredreport.js`, insert your OpenAI API Key.
8. There are a few hardcoded IPs in the code and Unity project. Adjust them accordingly: the ws1 and ws2 variables in webapp/js/main.js, which are IP addresses of computers running the Unity projects, and the Echo Server Behavior component of the ProjectorManager GameObject should point to the computer running the web app.
9. Run the Unity project connected to a Microsoft Hololens 2 device with Holographic remoting, and run the web app on the same or a separate computer.

# Acknowledgement
Please refer to NOTICE.md for libraries used by this project.

# Disclaimer
This source code is provided as part of a research project and is intended for educational and research purposes only. Users are free to use this code for commercial purposes with respect to MIT License. However, The authors and contributors of this project make no warranties, express or implied, and assume no liability for damages or losses resulting from the use or misuse of this code. Users are responsible for ensuring that the code meets their needs and operates correctly in their environment. This code may contain bugs, errors, or inaccuracies and is subject to change without notice. By using this code, you agree to these terms and acknowledge that you understand and accept this disclaimer.

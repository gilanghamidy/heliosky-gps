# Heliosky.IoT.GPS Library
Library for using u-Blox NEO-6M GPS module in Raspberry Pi and Windows 10 IoT Core. This library is built using Universal Windows Platform (UWP) which is suitable for developing any location-based IoT app for Windows 10 IoT Core. Currently the library only implements minimal working mechanism to fetch geoposition data received by the GPS module. Additional data will be added at later time as the library is built with modularity and extensibility in mind, which will ease our time to develop additional data type supported by u-Blox NEO-6M GPS module.

## Major Features
- Reading serial data from u-Blox NEO-6M GPS module
- Asynchronous mechanism, suitable for usage in non-blocking context such as UI application
- Automatic probing baud rate
- Extensible pattern for internal components, which will be useful and quick to implement additional data/message types supported by the GPS module
- Event-based incoming data notification, for periodic data received from the module

## Currently Available GPS Data/Message Type
Below is the list of classes that represent the Data/Message supported by the GPS module, as described by the [protocol specification](https://www.u-blox.com/sites/default/files/products/documents/u-blox6_ReceiverDescrProtSpec_%28GPS.G6-SW-10018%29_Public.pdf?utm_source=en%2Fimages%2Fdownloads%2FProduct_Docs%2Fu-blox6_ReceiverDescriptionProtocolSpec_%28GPS.G6-SW-10018%29.pdf). The list below is the exact namespace structure in the library

1. Configuration
  1. Port: Port configuration (serial configuration, baud rate, protocol)
  2. Message: Configure the module to send periodic data/message
  3. Rate: Configure how often the module process positioning data
2. Monitor
  1. ReceiverStatus: Some kind of ping
3. Navigation
  1. Clock: GPS clock
  2. DOP: Dillution of Precision
  3. ECEFPosition: Positioning using ECEF format (X, Y, Z spherical coordinat)
  4. GeodeticPosition: Positioning using geodetic format (Latitude, Longitude, MSL)
  5. SpaceVehicleInfo: Information about sattelites seen by the module

## Sample Projects
The sample project is included in this repository. It can only be run in Windows 10 IoT Core. Currently it performs demonstration on how to use the GPS library in conjunction with Map control. The pinpoint icon is retrieved from Microsoft's [MapControl sample project](https://github.com/Microsoft/Windows-universal-samples/tree/master/Samples/MapControl).

## Testability
The project has small unit test code to test the parsing components only. To test the GPS functionality, we have to use the actual application running the gps on the actual device. Currently Microsoft Test framework does not support performing unit test on ARM remote device.

The sample project is tested and run on Windows 10 IoT Core in Raspberry Pi 3 device using u-Blox NEO-6M GPS Module.

## Additional Projects
Heliosky.IoT.GPS.Legacy library contains legacy codes of previous development using NMEA GPS message. I switch implementation to use proprietary UBX protocol instead for more efficient data transmission.

Heliosky.IoT.GPS.Service project is a planned sample project to develop service application which utilizes the GPS library. It is currently not yet developed.

## License
The Heliosky.IoT.GPS library and Heliosky.IoT.GPS.Legacy library is licensed under the terms of the GNU Lesser General Public License as published by the Free Software Foundation

The Heliosky.IoT.GPS.SampleApp sample project, Heliosky.IoT.GPS.Service project, and Heliosky.IoT.GPS.Test project is licensed under The MIT License

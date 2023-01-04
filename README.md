# VersionWriter
A command-line tool for generating version files to use with [CnCNet Client](https://github.com/CnCNet/xna-cncnet-client) updater.

Details on how the tool works can be found in client's [updater documentation](https://github.com/CnCNet/xna-cncnet-client/blob/develop/Docs/Updater.md).

## Acknowledgements

- [Starkku.Utilities](https://github.com/Starkku/Starkku.Utilities) is used for logging & INI file handling.
- [ILMerge](https://github.com/dotnet/ILMerge) & [ILMerge.MSBuild.Task](https://github.com/emerbrito/ILMerge-MSBuild-Task) NuGet packages are included and used to merge assemblies into single executable. 
- 7Zip's official C# LZMA implementation is used to implement archive compression.

## License
See [LICENSE.txt](LICENSE.txt).
# VersionWriter
A command-line tool for generating version files to use with [CnCNet Client](https://github.com/CnCNet/xna-cncnet-client) updater.

It is currently distributed as part of [CnCNet Client Yuri's Revenge Mod Base](https://github.com/Starkku/cncnet-client-mod-base) project and the [updater documentation](https://github.com/Starkku/cncnet-client-mod-base/blob/master/Documentation/Updater.md) included contains more insight into the tool's workings.

## Acknowledgements

- [Starkku.Utilities](https://github.com/Starkku/Starkku.Utilities) is used for logging & INI file handling.
- [ILMerge](https://github.com/dotnet/ILMerge) & [ILMerge.MSBuild.Task](https://github.com/emerbrito/ILMerge-MSBuild-Task) NuGet packages are included and used to merge assemblies into single executable. 
- 7Zip's official C# LZMA implementation is used to implement archive compression.

## License
See [LICENSE.txt](LICENSE.txt).
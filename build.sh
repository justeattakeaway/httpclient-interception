#!/usr/bin/env bash

root=$(cd "$(dirname "$0")"; pwd -P)
artifacts=$root/artifacts
configuration=Release

restorePackages=0
skipTests=0

while :; do
    if [ $# -le 0 ]; then
        break
    fi

    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        -\?|-h|--help)
            echo "./build.sh [--restore-packages] [--skip-tests]"
            exit 1
            ;;

        --restore-packages)
            restorePackages=1
            ;;

        --skip-tests)
            skipTests=1
            ;;

        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $1"
            ;;
    esac

    shift
done

export CLI_VERSION="2.0.3"
export DOTNET_INSTALL_DIR="$root/.dotnetcli"
export PATH="$DOTNET_INSTALL_DIR:$PATH"

dotnet_version=$(dotnet --version)

if [ "$dotnet_version" != "$CLI_VERSION" ]; then
    curl -sSL https://raw.githubusercontent.com/dotnet/cli/release/2.0.0/scripts/obtain/dotnet-install.sh | bash /dev/stdin --version "$CLI_VERSION" --install-dir "$DOTNET_INSTALL_DIR"
fi

if [ $restorePackages == 1 ]; then
    dotnet restore ./HttpClientInterception.sln --verbosity minimal || exit 1
fi

dotnet build ./src/HttpClientInterception/JustEat.HttpClientInterception.csproj --output $artifacts --configuration $configuration --framework "netstandard1.3" || exit 1

if [ $skipTests == 0 ]; then
    dotnet test ./tests/HttpClientInterception.Tests/JustEat.HttpClientInterception.Tests.csproj --output $artifacts --configuration $configuration || exit 1
fi

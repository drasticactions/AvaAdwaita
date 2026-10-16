import { dotnet } from './_framework/dotnet.js'

const runtime = await dotnet.withApplicationArguments(...window.location.search.slice(1).split('&').filter(Boolean)).create();
await runtime.runMain();

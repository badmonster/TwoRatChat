# README #

1. Для компиляции потребуется MS VS 2022+MS но можно собрать где угодно, это же исходники.
2. Проект зависит от 

* Awesomium
* Newtonsoft.Json
* NHotkey
* NAudio
* EngineIoClientDotNet - https://github.com/Quobject/EngineIoClientDotNet
* NHttp
* CefSharp (вариант заюзать вместо него System.Diagnostics.Process.Start)
* TwitchLib + all libs и ещё чего-то.

3. Остальное криво форкнуто и добавлено в проект (dotIRC,  ̶ ̶j̶a̶b̶b̶e̶r̶-̶n̶e̶t - остался, ̶но не использвуется) потому что потребовались изменения в коде.
4. Для запуска не потребуется папка DATA от оригинального крысочата (который качался с ̶h̶t̶t̶p̶:̶/̶/̶t̶w̶o̶r̶a̶t̶c̶h̶a̶t̶.̶c̶o̶m̶) там находились всякие скины и скрипты чата. (стартует и так).
5. Для голоса потребуется SpeechPlatformRuntime + Windows 10, тогда будут работать и голосовые оповещения, и управление голосом в играх (нужно будет править конфиг Commands.xml).

# Для тех кто тут случайно #
Это программа агрегатор чатов, умеет (или должна уметь) читать чаты с разных источников и показывать их в одном месте. Так же есть голосовалка, черные/белые списки. Поддержка скинов и http-server для хостинга чата для любого устройства в сети. Что-то там еще есть, я уже и не помню.
Читает:

* youtube
* twitch
* goodgame
* peka2tv (formely sc2tv)
* others...

Лицензия: MIT

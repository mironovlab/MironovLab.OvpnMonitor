SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

CREATE DATABASE IF NOT EXISTS `openvpn` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE `openvpn`;
CREATE TABLE `active_sessions` (
`bytes_in` bigint
,`bytes_out` bigint
,`common_name` varchar(128)
,`connected` timestamp
,`ip_address` varchar(64)
,`last_updated` timestamp
,`platform` varchar(64)
);

CREATE TABLE `intermediate_data` (
  `id` int NOT NULL,
  `session_id` int NOT NULL,
  `date` date NOT NULL,
  `bytes_in` bigint NOT NULL,
  `bytes_out` bigint NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `sessions` (
  `id` int NOT NULL,
  `user_id` int NOT NULL,
  `session_id` int NOT NULL,
  `connected` timestamp NOT NULL,
  `ip_address` varchar(64) NOT NULL,
  `platform` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `bytes_in` bigint NOT NULL,
  `bytes_out` bigint NOT NULL,
  `last_updated` timestamp NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `users` (
  `id` int NOT NULL,
  `common_name` varchar(128) NOT NULL,
  `ip_address` varchar(64) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
DROP TABLE IF EXISTS `active_sessions`;

CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `active_sessions`  AS WITH     `active` as (select `sessions`.`user_id` AS `user_id`,max(`sessions`.`last_updated`) AS `last_updated` from `sessions` where (`sessions`.`last_updated` > (now() - interval 1 minute)) group by `sessions`.`user_id`) select `users`.`common_name` AS `common_name`,`sessions`.`connected` AS `connected`,`sessions`.`ip_address` AS `ip_address`,`sessions`.`platform` AS `platform`,`sessions`.`bytes_in` AS `bytes_in`,`sessions`.`bytes_out` AS `bytes_out`,`sessions`.`last_updated` AS `last_updated` from ((`active` left join `sessions` on(((`active`.`user_id` = `sessions`.`user_id`) and (`active`.`last_updated` = `sessions`.`last_updated`)))) left join `users` on((`sessions`.`user_id` = `users`.`id`)))  ;


ALTER TABLE `intermediate_data`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_date` (`date`),
  ADD KEY `idx_session_id` (`session_id`) USING BTREE;

ALTER TABLE `sessions`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `idx_user_session` (`user_id`,`session_id`,`connected`) USING BTREE,
  ADD KEY `idx_connected` (`connected`),
  ADD KEY `idx_last_updated` (`last_updated`);

ALTER TABLE `users`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `common_name_idx` (`common_name`);


ALTER TABLE `intermediate_data`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

ALTER TABLE `sessions`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;

ALTER TABLE `users`
  MODIFY `id` int NOT NULL AUTO_INCREMENT;


ALTER TABLE `intermediate_data`
  ADD CONSTRAINT `sessions` FOREIGN KEY (`session_id`) REFERENCES `sessions` (`id`) ON DELETE RESTRICT ON UPDATE RESTRICT;

ALTER TABLE `sessions`
  ADD CONSTRAINT `users` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE ON UPDATE RESTRICT;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;

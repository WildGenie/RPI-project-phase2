SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='TRADITIONAL,ALLOW_INVALID_DATES';

-- -----------------------------------------------------
-- Schema mydb
-- -----------------------------------------------------
-- -----------------------------------------------------
-- Schema iContrAll
-- -----------------------------------------------------
CREATE SCHEMA IF NOT EXISTS `iContrAll` DEFAULT CHARACTER SET latin1 ;
USE `iContrAll` ;

-- -----------------------------------------------------
-- Table `iContrAll`.`ActionLists`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `iContrAll`.`ActionLists` ;

CREATE TABLE IF NOT EXISTS `iContrAll`.`ActionLists` (
  `Id` VARCHAR(38) NOT NULL,
  `Name` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE INDEX `Name_UNIQUE` (`Name` ASC))
ENGINE = InnoDB
DEFAULT CHARACTER SET = latin1;


-- -----------------------------------------------------
-- Table `iContrAll`.`DeviceTypes`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `iContrAll`.`DeviceTypes` ;

CREATE TABLE IF NOT EXISTS `iContrAll`.`DeviceTypes` (
  `Id` VARCHAR(3) NOT NULL,
  `Name` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE INDEX `Name_UNIQUE` (`Name` ASC))
ENGINE = InnoDB
DEFAULT CHARACTER SET = latin1;

INSERT INTO `iContrAll`.`DeviceTypes` VALUES ('LC1', 'Lámpa');
INSERT INTO `iContrAll`.`DeviceTypes` VALUES ('OC1', 'Redony');

-- -----------------------------------------------------
-- Table `iContrAll`.`ActionTypes`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `iContrAll`.`ActionTypes` ;

CREATE TABLE IF NOT EXISTS `iContrAll`.`ActionTypes` (
  `Id` INT(11) NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(45) NOT NULL,
  `DeviceType` VARCHAR(3) NOT NULL,
  PRIMARY KEY (`Id`),
  INDEX `DevType_idx` (`DeviceType` ASC),
  CONSTRAINT `DevType`
    FOREIGN KEY (`DeviceType`)
    REFERENCES `iContrAll`.`DeviceTypes` (`Id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB
AUTO_INCREMENT = 19
DEFAULT CHARACTER SET = latin1;

INSERT INTO `iContrAll`.`ActionTypes` VALUES (1, 'on', 'LC1');
INSERT INTO `iContrAll`.`ActionTypes` VALUES (2, 'off', 'LC1');
INSERT INTO `iContrAll`.`ActionTypes` VALUES (3, '0', 'OC1');
INSERT INTO `iContrAll`.`ActionTypes` VALUES (4, '100', 'OC1');

-- -----------------------------------------------------
-- Table `iContrAll`.`Devices`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `iContrAll`.`Devices` ;

CREATE TABLE IF NOT EXISTS `iContrAll`.`Devices` (
  `Id` VARCHAR(8) NOT NULL,
  `Channel` INT(11) NOT NULL,
  `Name` VARCHAR(45) NOT NULL,
  `Timer` INT(11) NULL DEFAULT NULL,
  `Voltage` INT(11) NULL DEFAULT NULL,
  `DeviceType` VARCHAR(3) NOT NULL,
  PRIMARY KEY (`Id`, `Channel`),
  INDEX `DeviceType_idx` (`DeviceType` ASC),
  CONSTRAINT `DeviceType`
    FOREIGN KEY (`DeviceType`)
    REFERENCES `iContrAll`.`DeviceTypes` (`Id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB
DEFAULT CHARACTER SET = latin1;


-- -----------------------------------------------------
-- Table `iContrAll`.`Actions`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `iContrAll`.`Actions` ;

CREATE TABLE IF NOT EXISTS `iContrAll`.`Actions` (
  `DeviceId` VARCHAR(10) NOT NULL,
  `DeviceChannel` INT(11) NOT NULL,
  `ActionTypeId` INT(11) NOT NULL,
  `ActionListId` VARCHAR(38) NOT NULL,
  `OrderNumber` INT(11) NULL DEFAULT '1',
  PRIMARY KEY (`DeviceId`, `DeviceChannel`, `ActionTypeId`, `ActionListId`),
  INDEX `ActionTypeKey_idx` (`ActionTypeId` ASC),
  INDEX `ActionListKey_idx` (`ActionListId` ASC),
  CONSTRAINT `ActionListKey`
    FOREIGN KEY (`ActionListId`)
    REFERENCES `iContrAll`.`ActionLists` (`Id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `ActionTypeKey`
    FOREIGN KEY (`ActionTypeId`)
    REFERENCES `iContrAll`.`ActionTypes` (`Id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION,
  CONSTRAINT `DeviceToActionKey`
    FOREIGN KEY (`DeviceId` , `DeviceChannel`)
    REFERENCES `iContrAll`.`Devices` (`Id` , `Channel`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION)
ENGINE = InnoDB
DEFAULT CHARACTER SET = latin1;


-- -----------------------------------------------------
-- Table `iContrAll`.`Places`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `iContrAll`.`Places` ;

CREATE TABLE IF NOT EXISTS `iContrAll`.`Places` (
  `Id` VARCHAR(38) NOT NULL,
  `Name` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`))
ENGINE = InnoDB
DEFAULT CHARACTER SET = latin1;


-- -----------------------------------------------------
-- Table `iContrAll`.`DevicesInPlaces`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `iContrAll`.`DevicesInPlaces` ;

CREATE TABLE IF NOT EXISTS `iContrAll`.`DevicesInPlaces` (
  `DeviceId` VARCHAR(45) NOT NULL,
  `DeviceChannel` INT(11) NOT NULL,
  `PlaceId` VARCHAR(38) NOT NULL,
  PRIMARY KEY (`DeviceId`, `DeviceChannel`, `PlaceId`),
  INDEX `PlaceId_idx` (`PlaceId` ASC),
  CONSTRAINT `DeviceKey`
    FOREIGN KEY (`DeviceId` , `DeviceChannel`)
    REFERENCES `iContrAll`.`Devices` (`Id` , `Channel`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION,
  CONSTRAINT `PlaceKey`
    FOREIGN KEY (`PlaceId`)
    REFERENCES `iContrAll`.`Places` (`Id`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION)
ENGINE = InnoDB
DEFAULT CHARACTER SET = latin1;


-- -----------------------------------------------------
-- Table `iContrAll`.`Statuses`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `iContrAll`.`Statuses` ;

CREATE TABLE IF NOT EXISTS `iContrAll`.`Statuses` (
  `DeviceId` VARCHAR(10) NOT NULL,
  `DeviceChannel` INT(11) NOT NULL,
  `State` INT(2) NULL DEFAULT NULL,
  `Value` INT(11) NULL DEFAULT NULL,
  `Power` INT(11) NULL DEFAULT NULL,
  PRIMARY KEY (`DeviceId`, `DeviceChannel`),
  CONSTRAINT `DeviceKeyFromStatues`
    FOREIGN KEY (`DeviceId` , `DeviceChannel`)
    REFERENCES `iContrAll`.`Devices` (`Id` , `Channel`)
    ON DELETE CASCADE
    ON UPDATE NO ACTION)
ENGINE = InnoDB
DEFAULT CHARACTER SET = latin1;

DROP TABLE IF EXISTS `iContrAll`.`Timers` ;

CREATE TABLE `iContrAll`.`Timers` (

  `deviceId` VARCHAR(8) NOT NULL,

  `channel` INT(11) NOT NULL,

  `startTime` VARCHAR(4) NOT NULL,

  `endTime` VARCHAR(4) NOT NULL,

  `addDate` DATE NOT NULL,

  PRIMARY KEY (`deviceId`, `channel`));



SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
